import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import type { AccountDto } from '../types/api'
import type { OperationRequest } from '../types/operation'
import OperationPanel from './OperationPanel.vue'

const checking: AccountDto = { id: 'checking-id', name: 'Checking', number: '****1234', balance: 1000 }
const savings: AccountDto = { id: 'savings-id', name: 'Savings', number: '****5678', balance: 5000 }

function mountPanel() {
  return mount(OperationPanel, {
    props: { accounts: [checking, savings], selectedId: checking.id, pending: false },
  })
}

function submitButton(wrapper: ReturnType<typeof mountPanel>) {
  return wrapper.get('button[type="submit"]')
}

function emittedRequests(wrapper: ReturnType<typeof mountPanel>): OperationRequest[] {
  return (wrapper.emitted('submit') ?? []).map((args) => args[0] as OperationRequest)
}

describe('OperationPanel', () => {
  it('disables submit for an invalid amount', async () => {
    const wrapper = mountPanel()

    await wrapper.get('input').setValue('0')

    expect(submitButton(wrapper).attributes('disabled')).toBeDefined()
    expect(wrapper.text()).toContain('Amount must be greater than zero.')
  })

  it('always targets the other account in a transfer, and swap reverses the direction', async () => {
    const wrapper = mountPanel()
    await wrapper.findAll('[role="tab"]')[2].trigger('click')
    await wrapper.get('input').setValue('10')

    await wrapper.get('form').trigger('submit')
    expect(emittedRequests(wrapper).at(-1)).toEqual({
      kind: 'transfer',
      fromAccountId: checking.id,
      toAccountId: savings.id,
      amount: 10,
    })

    await wrapper.get('button[aria-label="Swap direction"]').trigger('click')
    await wrapper.get('form').trigger('submit')
    expect(emittedRequests(wrapper).at(-1)).toEqual({
      kind: 'transfer',
      fromAccountId: savings.id,
      toAccountId: checking.id,
      amount: 10,
    })
  })

  it('emits the operation with the parsed amount', async () => {
    const wrapper = mountPanel()
    await wrapper.get('input').setValue('25.50')

    await wrapper.get('form').trigger('submit')

    expect(emittedRequests(wrapper)).toEqual([{ kind: 'deposit', amount: 25.5 }])
  })
})
