import { useEffect, useRef, useState } from 'react';
import { fileToPhotoDataUrl } from '../utils/photoUtils';
import { PersonAvatar } from './PersonAvatar';

type PhotoCaptureProps = {
  name: string;
  value?: string | null;
  onChange: (photoData: string | null) => void;
};

export function PhotoCapture({ name, value, onChange }: PhotoCaptureProps) {
  const fileRef = useRef<HTMLInputElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const [cameraOpen, setCameraOpen] = useState(false);
  const [error, setError] = useState('');
  const streamRef = useRef<MediaStream | null>(null);

  useEffect(() => {
    return () => {
      streamRef.current?.getTracks().forEach((track) => track.stop());
    };
  }, []);

  async function openCamera() {
    setError('');
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'user', width: { ideal: 640 }, height: { ideal: 480 } },
        audio: false,
      });
      streamRef.current = stream;
      setCameraOpen(true);
      requestAnimationFrame(() => {
        if (videoRef.current) {
          videoRef.current.srcObject = stream;
          void videoRef.current.play();
        }
      });
    } catch {
      setError('Não foi possível acessar a câmera. Verifique as permissões do navegador.');
    }
  }

  function closeCamera() {
    streamRef.current?.getTracks().forEach((track) => track.stop());
    streamRef.current = null;
    setCameraOpen(false);
  }

  async function capturePhoto() {
    const video = videoRef.current;
    if (!video) return;

    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.drawImage(video, 0, 0);

    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', 0.9));
    if (!blob) return;

    try {
      const dataUrl = await fileToPhotoDataUrl(blob);
      onChange(dataUrl);
      closeCamera();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao capturar foto.');
    }
  }

  async function handleFileChange(file: File | undefined) {
    if (!file) return;
    setError('');
    try {
      const dataUrl = await fileToPhotoDataUrl(file);
      onChange(dataUrl);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar imagem.');
    }
  }

  return (
    <div className="photo-capture">
      <div className="photo-capture-preview">
        <PersonAvatar name={name || '?'} photoData={value} size={96} />
      </div>
      <div className="photo-capture-actions">
        <button type="button" className="btn btn-secondary btn-sm" onClick={() => fileRef.current?.click()}>
          Escolher arquivo
        </button>
        <button type="button" className="btn btn-secondary btn-sm" onClick={openCamera}>
          Tirar foto
        </button>
        {value && (
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => onChange(null)}>
            Remover
          </button>
        )}
      </div>
      <input
        ref={fileRef}
        type="file"
        accept="image/*"
        hidden
        onChange={(e) => {
          void handleFileChange(e.target.files?.[0]);
          e.target.value = '';
        }}
      />
      {error && <p className="form-error" style={{ margin: '8px 0 0' }}>{error}</p>}

      {cameraOpen && (
        <div className="camera-overlay" role="presentation" onClick={closeCamera}>
          <div className="camera-panel" onClick={(e) => e.stopPropagation()}>
            <h3>Tirar foto</h3>
            <video ref={videoRef} className="camera-video" playsInline muted />
            <div className="camera-panel-actions">
              <button type="button" className="btn btn-secondary" onClick={closeCamera}>Cancelar</button>
              <button type="button" className="btn" onClick={capturePhoto}>Capturar</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
