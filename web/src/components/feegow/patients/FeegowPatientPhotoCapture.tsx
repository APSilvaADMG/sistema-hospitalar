import { useEffect, useRef, useState, type ChangeEvent } from 'react';
import { PersonAvatar } from '../../PersonAvatar';
import { fileToPhotoDataUrl } from '../../../utils/photoUtils';

type Props = {
  name: string;
  photoData?: string;
  onChange: (photoData: string | undefined) => void;
};

export function FeegowPatientPhotoCapture({ name, photoData, onChange }: Props) {
  const fileRef = useRef<HTMLInputElement>(null);
  const videoRef = useRef<HTMLVideoElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const [cameraOpen, setCameraOpen] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => () => {
    streamRef.current?.getTracks().forEach((track) => track.stop());
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
      setError('Não foi possível acessar a câmera.');
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

  async function handlePhotoFile(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    setError('');
    try {
      const dataUrl = await fileToPhotoDataUrl(file);
      onChange(dataUrl);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar imagem.');
    } finally {
      event.target.value = '';
    }
  }

  return (
    <div className="feegow-patient-photo-col">
      <div className="feegow-patient-photo-box">
        {photoData ? (
          <img src={photoData} alt="" className="feegow-patient-photo-img" />
        ) : (
          <>
            <PersonAvatar name={name || 'Paciente'} size={116} />
            <span className="feegow-patient-photo-label">Sem foto</span>
          </>
        )}
      </div>

      <input
        ref={fileRef}
        type="file"
        accept="image/*"
        className="feegow-patient-photo-input"
        onChange={handlePhotoFile}
      />

      <div className="feegow-patient-photo-actions">
        <button
          type="button"
          className="feegow-patient-photo-btn"
          onClick={() => fileRef.current?.click()}
          title="Escolher arquivo"
          aria-label="Escolher arquivo de imagem"
        >
          📁
        </button>
        <button
          type="button"
          className="feegow-patient-photo-btn"
          onClick={() => { openCamera().catch(console.error); }}
          title="Tirar foto"
          aria-label="Tirar foto com a câmera"
        >
          📷
        </button>
        {photoData ? (
          <button
            type="button"
            className="feegow-patient-photo-btn feegow-patient-photo-btn-remove"
            onClick={() => onChange(undefined)}
            title="Remover foto"
            aria-label="Remover foto"
          >
            ✕
          </button>
        ) : null}
      </div>

      {error ? <small className="feegow-patient-photo-error">{error}</small> : null}

      {cameraOpen ? (
        <div className="camera-overlay" role="presentation" onClick={closeCamera}>
          <div className="camera-panel" onClick={(e) => e.stopPropagation()}>
            <h3>Tirar foto do paciente</h3>
            <video ref={videoRef} className="camera-video" playsInline muted />
            <div className="camera-panel-actions">
              <button type="button" className="feegow-form-btn-cancel" onClick={closeCamera}>
                Cancelar
              </button>
              <button type="button" className="feegow-patient-save-btn" onClick={() => { capturePhoto().catch(console.error); }}>
                Capturar
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
