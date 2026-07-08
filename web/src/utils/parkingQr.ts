import QRCode from 'qrcode';

export function parkingQrPayload(sessionId: string, serverPayload?: string) {
  return serverPayload ?? `HMS-PARK:${sessionId}`;
}

export async function parkingQrDataUrl(payload: string, size = 160) {
  return QRCode.toDataURL(payload, {
    width: size,
    margin: 1,
    errorCorrectionLevel: 'M',
  });
}
