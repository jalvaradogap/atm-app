/** What the operation panel asks the screen to do. A transfer names both accounts, a deposit or withdrawal does not. */
export type OperationRequest =
  | { kind: 'deposit'; amount: number }
  | { kind: 'withdraw'; amount: number }
  | { kind: 'transfer'; fromAccountId: string; toAccountId: string; amount: number }

export type OperationKind = OperationRequest['kind']
