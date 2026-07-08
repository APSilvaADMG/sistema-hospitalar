import { useEffect, useState, type FormEvent } from 'react';
import type { GuideHubListItemDto } from '../../api/client';
import { Modal } from '../Modal';

type Props = {
  open: boolean;
  guide: GuideHubListItemDto | null;
  saving?: boolean;
  onClose: () => void;
  onConfirm: (authorizationNumber?: string) => void;
};

export function SusAuthorizeModal({ open, guide, saving, onClose, onConfirm }: Props) {
  const [authorizationNumber, setAuthorizationNumber] = useState('');

  useEffect(() => {
    if (open) setAuthorizationNumber('');
  }, [open, guide?.id]);

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onConfirm(authorizationNumber.trim() || undefined);
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Autorizar guia SUS"
      subtitle={guide ? `${guide.guideNumber} — ${guide.patientName}` : undefined}
      width="md"
    >
      <form className="form-grid" onSubmit={handleSubmit}>
        <div className="form-field full">
          <label htmlFor="susAuthNumber">Número da autorização</label>
          <input
            id="susAuthNumber"
            type="text"
            value={authorizationNumber}
            onChange={(e) => setAuthorizationNumber(e.target.value)}
            placeholder="Opcional — informe se já recebido do gestor"
            autoFocus
          />
          <p className="form-hint">
            A guia passará para o status <strong>Autorizada</strong>. Deixe em branco se ainda não houver número.
          </p>
        </div>
        <div className="form-field full modal-actions">
          <button type="button" className="btn btn-secondary" onClick={onClose} disabled={saving}>
            Cancelar
          </button>
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Autorizando…' : 'Confirmar autorização'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
