/** Mirrors AmountRules.MaxAmount in the domain. The backend still enforces it. */
export const AMOUNT_MAX = 1_000_000

export type AmountValidation = { valid: true; value: number } | { valid: false; message: string }

/**
 * Checks a typed amount before anything is sent: greater than zero, at most 2 decimal places,
 * and no more than AMOUNT_MAX. Returns a reason the user can read.
 */
export function validateAmount(input: string): AmountValidation {
  const text = input.trim()

  if (text === '') {
    return invalid('Enter an amount.')
  }

  if (!/^-?\d+(\.\d+)?$/.test(text)) {
    return invalid('Enter a valid amount, such as 25 or 25.50.')
  }

  const value = Number(text)

  if (value <= 0) {
    return invalid('Amount must be greater than zero.')
  }

  const decimalPlaces = text.split('.')[1]?.length ?? 0
  if (decimalPlaces > 2) {
    return invalid('Use at most 2 decimal places.')
  }

  if (value > AMOUNT_MAX) {
    return invalid('Amount cannot exceed 1,000,000.')
  }

  return { valid: true, value }
}

function invalid(message: string): AmountValidation {
  return { valid: false, message }
}
