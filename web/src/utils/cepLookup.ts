export type CepAddress = {
  addressStreet: string;
  addressNeighborhood: string;
  addressCity: string;
  addressState: string;
  addressComplement?: string;
};

export function normalizeCepDigits(cep: string): string {
  return cep.replace(/\D/g, '').slice(0, 8);
}

export function formatCep(cep: string): string {
  const digits = normalizeCepDigits(cep);
  if (digits.length <= 5) return digits;
  return `${digits.slice(0, 5)}-${digits.slice(5)}`;
}

export async function fetchAddressByCep(cep: string): Promise<CepAddress> {
  const digits = normalizeCepDigits(cep);
  if (digits.length !== 8) {
    throw new Error('Informe um CEP com 8 dígitos.');
  }

  const response = await fetch(`https://viacep.com.br/ws/${digits}/json/`);
  if (!response.ok) {
    throw new Error('Não foi possível consultar o CEP. Tente novamente.');
  }

  const data = (await response.json()) as {
    erro?: boolean;
    logradouro?: string;
    bairro?: string;
    localidade?: string;
    uf?: string;
    complemento?: string;
  };

  if (data.erro) {
    throw new Error('CEP não encontrado.');
  }

  return {
    addressStreet: data.logradouro ?? '',
    addressNeighborhood: data.bairro ?? '',
    addressCity: data.localidade ?? '',
    addressState: data.uf ?? '',
    addressComplement: data.complemento || undefined,
  };
}
