<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, useTemplateRef } from 'vue'
import AccountCard from './components/AccountCard.vue'
import OperationPanel from './components/OperationPanel.vue'
import ServerBanner from './components/ServerBanner.vue'
import StatusMessage from './components/StatusMessage.vue'
import TransactionHistory from './components/TransactionHistory.vue'
import { useAccounts } from './composables/useAccounts'
import type { OperationRequest } from './types/operation'
import { formatMoney } from './utils/format'

const { accounts, selectedId, selected, history, pending, error, load, select, deposit, withdraw, transfer } =
  useAccounts()

const panel = useTemplateRef<InstanceType<typeof OperationPanel>>('panel')

/** Text for a successful operation, shown in the status region. */
const notice = ref<string | null>(null)
const highlightLatest = ref(false)
let highlightTimer: ReturnType<typeof setTimeout> | undefined

/** A server failure (no response, or a 5xx) gets the banner. Everything else is shown inline. */
const showBanner = computed(() => {
  const problem = error.value
  return problem !== null && (problem.unreachable || problem.status >= 500)
})

const inlineError = computed(() => {
  const problem = error.value
  if (problem === null || showBanner.value) {
    return null
  }
  return problem.detail ?? problem.title
})

const statusText = computed(() => inlineError.value ?? notice.value)
const statusTone = computed<'success' | 'error'>(() => (inlineError.value ? 'error' : 'success'))

onMounted(() => {
  void load()
})

onUnmounted(() => {
  clearTimeout(highlightTimer)
})

async function onSubmit(request: OperationRequest): Promise<void> {
  notice.value = null

  let succeeded: boolean
  if (request.kind === 'deposit') {
    succeeded = await deposit(request.amount)
  } else if (request.kind === 'withdraw') {
    succeeded = await withdraw(request.amount)
  } else {
    succeeded = await transfer(request.fromAccountId, request.toAccountId, request.amount)
  }

  if (!succeeded) {
    return
  }

  panel.value?.clear()
  notice.value = successText(request)
  flashLatest()
}

/** Balances are read after the refresh, so the message shows the new balances. */
function successText(request: OperationRequest): string {
  if (request.kind === 'transfer') {
    const amounts = accounts.value.map((a) => `${a.name}: ${formatMoney(a.balance)}`).join(' · ')
    return `Transferred ${formatMoney(request.amount)}. ${amounts}`
  }

  const verb = request.kind === 'deposit' ? 'Deposited' : 'Withdrew'
  const balance = selected.value ? ` New balance ${formatMoney(selected.value.balance)}.` : ''
  return `${verb} ${formatMoney(request.amount)}.${balance}`
}

function flashLatest(): void {
  highlightLatest.value = true
  clearTimeout(highlightTimer)
  highlightTimer = setTimeout(() => {
    highlightLatest.value = false
  }, 2000)
}
</script>

<template>
  <div class="mx-auto max-w-5xl p-4 md:p-8">
    <header class="mb-6">
      <h1 class="text-2xl font-bold tracking-tight">ATM</h1>
    </header>

    <ServerBanner :visible="showBanner" class="mb-6" @retry="load" />

    <div class="grid gap-6 md:grid-cols-2">
      <div class="space-y-6">
        <div class="grid grid-cols-2 gap-4">
          <template v-if="accounts.length > 0">
            <AccountCard
              v-for="account in accounts"
              :key="account.id"
              :account="account"
              :selected="account.id === selectedId"
              @select="select"
            />
          </template>
          <template v-else-if="pending">
            <div class="card h-28 animate-pulse" aria-label="Loading accounts" />
            <div class="card h-28 animate-pulse" />
          </template>
        </div>

        <OperationPanel
          v-if="selectedId && accounts.length >= 2"
          ref="panel"
          :accounts="accounts"
          :selected-id="selectedId"
          :pending="pending"
          @submit="onSubmit"
        />

        <StatusMessage :message="statusText" :tone="statusTone" />
      </div>

      <TransactionHistory
        :account-name="selected?.name ?? null"
        :transactions="history"
        :accounts="accounts"
        :loading="pending"
        :highlight-latest="highlightLatest"
      />
    </div>
  </div>
</template>
