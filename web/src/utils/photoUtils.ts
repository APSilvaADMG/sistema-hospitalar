const MAX_SIZE = 400;
const JPEG_QUALITY = 0.82;

export async function resizeImageToDataUrl(file: File | Blob): Promise<string> {
  const bitmap = await createImageBitmap(file);
  const scale = Math.min(1, MAX_SIZE / Math.max(bitmap.width, bitmap.height));
  const width = Math.round(bitmap.width * scale);
  const height = Math.round(bitmap.height * scale);

  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  const ctx = canvas.getContext('2d');
  if (!ctx) throw new Error('Não foi possível processar a imagem.');
  ctx.drawImage(bitmap, 0, 0, width, height);
  bitmap.close();

  return canvas.toDataURL('image/jpeg', JPEG_QUALITY);
}

export async function fileToPhotoDataUrl(file: File | Blob): Promise<string> {
  if (file instanceof File && !file.type.startsWith('image/')) {
    throw new Error('Selecione um arquivo de imagem válido.');
  }
  return resizeImageToDataUrl(file);
}
