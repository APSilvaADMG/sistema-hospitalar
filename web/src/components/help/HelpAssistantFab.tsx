import { useState } from 'react';
import { useLocation } from 'react-router-dom';
import { HelpAssistantChat } from './HelpArticlePanels';

export function HelpAssistantFab() {
  const { pathname } = useLocation();
  const [open, setOpen] = useState(false);

  if (pathname.startsWith('/ajuda') || pathname.startsWith('/portal-paciente')) {
    return null;
  }

  return (
    <div className="help-assistant-fab">
      {open ? (
        <div className="help-assistant-panel" role="dialog" aria-label="Assistente de ajuda">
          <div className="help-assistant-head">
            <span>Assistente de Ajuda</span>
            <button
              type="button"
              onClick={() => setOpen(false)}
              aria-label="Fechar"
              style={{ background: 'none', border: 'none', color: '#fff', cursor: 'pointer', fontSize: 18 }}
            >
              ×
            </button>
          </div>
          <HelpAssistantChat route={pathname} />
        </div>
      ) : null}
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-label={open ? 'Fechar assistente' : 'Abrir assistente de ajuda'}
        title="Assistente de Ajuda"
      >
        ?
      </button>
    </div>
  );
}
