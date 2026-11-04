<script setup lang="ts">
import { computed, ref, useId, useTemplateRef } from 'vue'
import type { AccountDto } from '../types/api'
import type { OperationKind, OperationRequest } from '../types/operation'
import { formatMoney } from '../utils/format'
import { validateAmount } from '../utils/amount'

const props = defineProps<{
  /** Both accounts. The form always works between these two. */
  accounts: AccountDto[]
  selectedId: string
  pending: boolean
}>()

const emit = defineEmits<{
  submit: [request: OperationRequest]
}>()

const tabs: { kind: OperationKind; label: string }[] = [
  { kind: 'deposit', label: 'Deposit' },
  { kind: 'withdraw', label: 'Withdraw' },
  { kind: 'transfer', label: 'Transfer' },
]

const uid = useId()
const kind = ref<OperationKind>('deposit')
const text = ref('')
/** Transfers only: false means selected → other, true means other → selected. */
const reversed = ref(false)
const tabButtons = useTemplateRef<HTMLButtonElement[]>('tabButtons')

const selected = computed(() => props.accounts.find((a) => a.id === props.selectedId) ?? null)
const other = computed(() => props.accounts.find((a) => a.id !== props.selectedId) ?? null)

const source = computed(() => (reversed.value ? other.value : selected.value))
const destination = computed(() => (reversed.value ? selected.value : other.value))

/** The account whose balance limits the amount. Deposits have no limit. */
const limitAccount = computed(() => (kind.value === 'transfer' ? source.value : selected.value))
const available = computed(() => (kind.value === 'deposit' ? null : (limitAccount.value?.balance ?? null)))

const check = computed(() => validateAmount(text.value))

/** The reason shown under the field. Empty input shows nothing yet, but the button stays disabled. */
const reason = computed<string | null>(() => {
  const result = check.value
  if (!result.valid) {
    return text.value.trim() === '' ? null : result.message
  }
  if (available.value !== null && result.value > available.value) {
    return `Exceeds available balance ${formatMoney(available.value)}`
  }
  return null
})

const canSubmit = computed(() => {
  if (props.pending || reason.value !== null || !check.value.valid) {
    return false
  }
  return kind.value !== 'transfer' || (source.value !== null && destination.value !== null)
})

const buttonLabel = computed(() => {
  if (props.pending) {
    return 'Processing…'
  }
  const verb = tabs.find((t) => t.kind === kind.value)?.label ?? 'Submit'
  return check.value.valid ? `${verb} ${formatMoney(check.value.value)}` : verb
})

const inputId = `${uid}-amount`
const reasonId = `${uid}-reason`
const availableId = `${uid}-available`
const panelId = `${uid}-panel`

function tabId(tabKind: OperationKind): string {
  return `${uid}-tab-${tabKind}`
}

function selectTab(tabKind: OperationKind): void {
  kind.value = tabKind
}

/** Left and right arrows move between tabs, as the WAI-ARIA tabs pattern expects. */
function onTabKeydown(event: KeyboardEvent, index: number): void {
  const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0
  if (step === 0) {
    return
  }
  event.preventDefault()
  const nextIndex = (index + step + tabs.length) % tabs.length
  selectTab(tabs[nextIndex].kind)
  tabButtons.value?.[nextIndex]?.focus()
}

function submit(): void {
  const result = check.value
  if (!canSubmit.value || !result.valid) {
    return
  }

  if (kind.value === 'transfer') {
    if (source.value === null || destination.value === null) {
      return
    }
    emit('submit', {
      kind: 'transfer',
      fromAccountId: source.value.id,
      toAccountId: destination.value.id,
      amount: result.value,
    })
    return
  }

  emit('submit', { kind: kind.value, amount: result.value })
}

/** Called by the screen after a successful operation, so the next amount starts empty. */
function clear(): void {
  text.value = ''
}

defineExpose({ clear })
</script>

<template>
  <section class="card" aria-label="Make a transaction">
    <div role="tablist" aria-label="Operation" class="mb-4 grid grid-cols-3 gap-1 rounded-lg bg-page p-1">
      <button
        v-for="(tab, index) in tabs"
        :key="tab.kind"
        ref="tabButtons"
        type="button"
        role="tab"
        :id="tabId(tab.kind)"
        :aria-selected="kind === tab.kind"
        :aria-controls="panelId"
        :tabindex="kind === tab.kind ? 0 : -1"
        class="rounded-md px-3 py-2 text-sm font-semibold transition focus-visible:outline-2 focus-visible:outline-accent"
        :class="kind === tab.kind ? 'bg-surface text-accent shadow-sm' : 'text-muted hover:text-ink'"
        @click="selectTab(tab.kind)"
        @keydown="onTabKeydown($event, index)"
      >
        {{ tab.label }}
      </button>
    </div>

    <form :id="panelId" role="tabpanel" :aria-labelledby="tabId(kind)" novalidate @submit.prevent="submit">
      <div v-if="kind === 'transfer'" class="mb-4 flex items-center justify-between gap-3 rounded-lg bg-page p-3 text-sm">
        <p>
          <span class="text-muted">From</span> <strong>{{ source?.name }}</strong>
          <span aria-hidden="true"> → </span>
          <span class="text-muted">To</span> <strong>{{ destination?.name }}</strong>
        </p>
        <button
          type="button"
          class="rounded-md bg-surface px-3 py-1 font-semibold ring-1 ring-slate-300 hover:ring-accent focus-visible:outline-2 focus-visible:outline-accent"
          aria-label="Swap direction"
          @click="reversed = !reversed"
        >
          ⇄ Swap
        </button>
      </div>

      <label :for="inputId" class="block text-sm font-medium text-muted">Amount</label>
      <div class="mt-1 flex items-center gap-2 rounded-lg bg-surface px-3 ring-1 ring-slate-300 focus-within:ring-2 focus-within:ring-accent">
        <span aria-hidden="true" class="text-lg text-muted">$</span>
        <input
          :id="inputId"
          v-model="text"
          type="text"
          inputmode="decimal"
          autocomplete="off"
          placeholder="0.00"
          class="w-full bg-transparent py-2 text-lg tabular-nums outline-none disabled:opacity-60"
          :disabled="pending"
          :aria-invalid="reason !== null"
          :aria-describedby="[reason ? reasonId : null, available !== null ? availableId : null].filter(Boolean).join(' ') || undefined"
        />
      </div>

      <p v-if="available !== null" :id="availableId" class="mt-2 text-sm text-muted">
        Available: <span class="font-semibold tabular-nums text-ink">{{ formatMoney(available) }}</span>
      </p>
      <p v-if="reason" :id="reasonId" class="mt-2 text-sm font-medium text-danger">{{ reason }}</p>

      <button
        type="submit"
        class="mt-5 w-full rounded-lg bg-accent px-4 py-3 font-semibold text-white shadow-sm transition hover:brightness-110 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent disabled:cursor-not-allowed disabled:opacity-50"
        :disabled="!canSubmit"
      >
        {{ buttonLabel }}
      </button>
    </form>
  </section>
</template>
