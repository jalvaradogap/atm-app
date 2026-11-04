<script setup lang="ts">
import type { AccountDto, TransactionDto } from '../types/api'
import { formatDateTime, formatMoney, formatSignedMoney, isCredit, transactionLabel } from '../utils/format'

defineProps<{
  accountName: string | null
  transactions: TransactionDto[]
  accounts: AccountDto[]
  loading: boolean
  /** Briefly true after an operation, so the newest row is noticed. */
  highlightLatest: boolean
}>()
</script>

<template>
  <section class="card" aria-labelledby="history-heading">
    <h2 id="history-heading" class="text-lg font-semibold">
      <span v-if="accountName">{{ accountName }} · </span>History
    </h2>

    <p v-if="loading && transactions.length === 0" class="mt-4 text-sm text-muted">Loading…</p>

    <p v-else-if="transactions.length === 0" class="mt-4 text-sm text-muted">
      No transactions yet. Make a deposit to get started.
    </p>

    <ol v-else class="mt-4 divide-y divide-slate-200">
      <li
        v-for="(transaction, index) in transactions"
        :key="transaction.id"
        class="flex items-start justify-between gap-4 py-3 transition-colors duration-700"
        :class="index === 0 && highlightLatest ? 'bg-accent/10' : ''"
      >
        <div class="min-w-0">
          <p class="font-medium">{{ transactionLabel(transaction, accounts) }}</p>
          <p v-if="transaction.description" class="text-sm text-ink">{{ transaction.description }}</p>
          <p class="text-sm text-muted">{{ formatDateTime(transaction.occurredAt) }}</p>
        </div>
        <div class="shrink-0 text-right tabular-nums">
          <p class="font-semibold" :class="isCredit(transaction.type) ? 'text-success' : 'text-danger'">
            {{ formatSignedMoney(transaction.type, transaction.amount) }}
          </p>
          <p class="text-sm text-muted">Balance {{ formatMoney(transaction.balanceAfter) }}</p>
        </div>
      </li>
    </ol>
  </section>
</template>
