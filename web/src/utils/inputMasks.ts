export function onlyDigits(value: string, max?: number): string {
  const digits = value.replace(/\D/g, '');
  return max !== undefined ? digits.slice(0, max) : digits;
}

export function formatCpfInput(value: string): string {
  const digits = onlyDigits(value, 11);
  if (digits.length <= 3) return digits;
  if (digits.length <= 6) return `${digits.slice(0, 3)}.${digits.slice(3)}`;
  if (digits.length <= 9) return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6)}`;
  return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`;
}

export function formatPhoneInput(value: string): string {
  const digits = onlyDigits(value, 11);
  if (!digits) return '';
  if (digits.length <= 2) return `(${digits}`;
  if (digits.length <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
  if (digits.length <= 10) {
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
  }
  return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7, 11)}`;
}

export function formatRgInput(value: string): string {
  const cleaned = value.replace(/[^\dXx]/g, '').slice(0, 10).toUpperCase();
  if (cleaned.length <= 2) return cleaned;
  if (cleaned.length <= 5) return `${cleaned.slice(0, 2)}.${cleaned.slice(2)}`;
  if (cleaned.length <= 8) return `${cleaned.slice(0, 2)}.${cleaned.slice(2, 5)}.${cleaned.slice(5)}`;
  return `${cleaned.slice(0, 2)}.${cleaned.slice(2, 5)}.${cleaned.slice(5, 8)}-${cleaned.slice(8)}`;
}

export function formatCnsInput(value: string): string {
  const digits = onlyDigits(value, 15);
  if (digits.length <= 3) return digits;
  if (digits.length <= 7) return `${digits.slice(0, 3)} ${digits.slice(3)}`;
  if (digits.length <= 11) return `${digits.slice(0, 3)} ${digits.slice(3, 7)} ${digits.slice(7)}`;
  return `${digits.slice(0, 3)} ${digits.slice(3, 7)} ${digits.slice(7, 11)} ${digits.slice(11)}`;
}

export function formatHeightInput(value: string): string {
  return onlyDigits(value, 3);
}

export function formatWeightInput(value: string): string {
  const raw = value.replace(/[^\d,]/g, '');
  const commaIndex = raw.indexOf(',');
  if (commaIndex === -1) return raw.slice(0, 3);
  const intPart = raw.slice(0, commaIndex).slice(0, 3);
  const decPart = raw.slice(commaIndex + 1).slice(0, 1);
  return decPart ? `${intPart},${decPart}` : `${intPart},`;
}

export function formatStateInput(value: string): string {
  return value.replace(/[^a-zA-Z]/g, '').toUpperCase().slice(0, 2);
}

export function isValidCpf(value: string): boolean {
  const cpf = onlyDigits(value, 11);
  if (cpf.length !== 11) return false;
  if (/^(\d)\1{10}$/.test(cpf)) return false;

  let sum = 0;
  for (let i = 0; i < 9; i += 1) {
    sum += Number(cpf[i]) * (10 - i);
  }
  let remainder = sum % 11;
  const firstDigit = remainder < 2 ? 0 : 11 - remainder;
  if (Number(cpf[9]) !== firstDigit) return false;

  sum = 0;
  for (let i = 0; i < 10; i += 1) {
    sum += Number(cpf[i]) * (11 - i);
  }
  remainder = sum % 11;
  const secondDigit = remainder < 2 ? 0 : 11 - remainder;
  return Number(cpf[10]) === secondDigit;
}

export function isValidCns(value: string): boolean {
  const cns = onlyDigits(value, 15);
  if (cns.length !== 15) return false;

  let sum = 0;
  for (let i = 0; i < cns.length; i += 1) {
    sum += Number(cns[i]) * (15 - i);
  }

  return sum % 11 === 0;
}
