import { describe, expect, it } from 'vitest'
import { validateAmount } from './amount'

describe('validateAmount', () => {
  it('accepts a positive amount with up to 2 decimal places', () => {
    expect(validateAmount('25')).toEqual({ valid: true, value: 25 })
    expect(validateAmount('25.50')).toEqual({ valid: true, value: 25.5 })
    expect(validateAmount('0.01')).toEqual({ valid: true, value: 0.01 })
  })

  it('accepts exactly the maximum', () => {
    expect(validateAmount('1000000')).toEqual({ valid: true, value: 1_000_000 })
  })

  it('rejects zero', () => {
    expect(validateAmount('0')).toEqual({ valid: false, message: 'Amount must be greater than zero.' })
  })

  it('rejects a negative amount', () => {
    expect(validateAmount('-5')).toEqual({ valid: false, message: 'Amount must be greater than zero.' })
  })

  it('rejects more than 2 decimal places', () => {
    expect(validateAmount('10.001')).toEqual({ valid: false, message: 'Use at most 2 decimal places.' })
  })

  it('asks for an amount when the input is empty or only spaces', () => {
    expect(validateAmount('')).toEqual({ valid: false, message: 'Enter an amount.' })
    expect(validateAmount('   ')).toEqual({ valid: false, message: 'Enter an amount.' })
  })

  it('rejects an amount above the maximum', () => {
    expect(validateAmount('1000000.01')).toEqual({ valid: false, message: 'Amount cannot exceed 1,000,000.' })
  })

  it('rejects text that is not a number', () => {
    expect(validateAmount('abc').valid).toBe(false)
  })
})
