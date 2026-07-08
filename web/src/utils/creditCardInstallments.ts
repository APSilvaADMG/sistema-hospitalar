export type PaymentInstallmentFormRow = {
  installmentNumber: number;
  amount: string;
  dueDate: string;
};

export function buildInstallmentRows(
  totalAmount: number,
  count: number,
  firstDueDate: string,
): PaymentInstallmentFormRow[] {
  if (!totalAmount || count < 2) return [];

  const centsTotal = Math.round(totalAmount * 100);
  const baseCents = Math.floor(centsTotal / count);
  const remainder = centsTotal - baseCents * count;
  const start = new Date(`${firstDueDate}T12:00:00`);

  return Array.from({ length: count }, (_, index) => {
    const date = new Date(start);
    date.setMonth(date.getMonth() + index);
    const cents = baseCents + (index === count - 1 ? remainder : 0);
    return {
      installmentNumber: index + 1,
      amount: (cents / 100).toFixed(2),
      dueDate: date.toISOString().slice(0, 10),
    };
  });
}

export function installmentRowsTotal(rows: PaymentInstallmentFormRow[]): number {
  return rows.reduce((sum, row) => sum + Number(row.amount || 0), 0);
}
