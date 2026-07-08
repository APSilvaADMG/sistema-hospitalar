export type ParsedNfeItem = {
  description: string;
  ncm: string;
  cfop: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
};

export type ParsedNfeXml = {
  supplierName: string;
  supplierCnpj: string;
  invoiceNumber: string;
  invoiceSeries: string;
  invoiceIssueDate: string;
  nfeAccessKey: string;
  freightAmount: number;
  discountAmount: number;
  totalAmount: number;
  items: ParsedNfeItem[];
};


function parseNumber(value: string): number {
  const normalized = value.replace(/\./g, '').replace(',', '.');
  const n = Number(normalized);
  return Number.isFinite(n) ? n : 0;
}

function parseIssueDate(raw: string): string {
  if (!raw) return '';
  const datePart = raw.slice(0, 10);
  if (/^\d{4}-\d{2}-\d{2}$/.test(datePart)) return datePart;
  const brMatch = raw.match(/^(\d{2})\/(\d{2})\/(\d{4})/);
  if (brMatch) return `${brMatch[3]}-${brMatch[2]}-${brMatch[1]}`;
  return '';
}

export function parseNfeXml(xmlText: string): ParsedNfeXml {
  const doc = new DOMParser().parseFromString(xmlText, 'application/xml');
  if (doc.querySelector('parsererror')) {
    throw new Error('XML inválido ou corrompido.');
  }

  const infNfe = doc.querySelector('infNFe, NFe infNFe, nfeProc NFe infNFe');
  const root = infNfe ?? doc.documentElement;

  const accessKey = (infNfe?.getAttribute('Id') ?? '').replace(/^NFe/i, '');
  const emit = root.querySelector('emit');
  const ide = root.querySelector('ide');
  const total = root.querySelector('total ICMSTot, total');

  const items: ParsedNfeItem[] = [];
  root.querySelectorAll('det').forEach((det) => {
    const prod = det.querySelector('prod');
    if (!prod) return;
    const quantity = parseNumber(prod.querySelector('qCom')?.textContent ?? prod.querySelector('qTrib')?.textContent ?? '0');
    const unitPrice = parseNumber(prod.querySelector('vUnCom')?.textContent ?? prod.querySelector('vUnTrib')?.textContent ?? '0');
    const lineTotal = parseNumber(prod.querySelector('vProd')?.textContent ?? '0');
    items.push({
      description: prod.querySelector('xProd')?.textContent?.trim() ?? '',
      ncm: prod.querySelector('NCM')?.textContent?.trim() ?? '',
      cfop: prod.querySelector('CFOP')?.textContent?.trim() ?? '',
      quantity,
      unitPrice,
      lineTotal: lineTotal > 0 ? lineTotal : quantity * unitPrice,
    });
  });

  if (items.length === 0) {
    throw new Error('Nenhum item de produto encontrado no XML.');
  }

  return {
    supplierName: emit?.querySelector('xNome')?.textContent?.trim() ?? '',
    supplierCnpj: emit?.querySelector('CNPJ')?.textContent?.trim() ?? emit?.querySelector('CPF')?.textContent?.trim() ?? '',
    invoiceNumber: ide?.querySelector('nNF')?.textContent?.trim() ?? '',
    invoiceSeries: ide?.querySelector('serie')?.textContent?.trim() ?? '',
    invoiceIssueDate: parseIssueDate(ide?.querySelector('dhEmi')?.textContent?.trim() ?? ide?.querySelector('dEmi')?.textContent?.trim() ?? ''),
    nfeAccessKey: accessKey,
    freightAmount: parseNumber(total?.querySelector('vFrete')?.textContent ?? '0'),
    discountAmount: parseNumber(total?.querySelector('vDesc')?.textContent ?? '0'),
    totalAmount: parseNumber(total?.querySelector('vNF')?.textContent ?? '0'),
    items,
  };
}
