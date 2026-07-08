type Props = {
  message?: string;
  onSend?: (message: string) => void;
};

export function FeegowRequisitionInteractions({ message, onSend }: Props) {
  return (
    <aside className="feegow-requisition-interactions">
      <header className="feegow-requisition-interactions-head">
        <span className="feegow-requisition-interactions-icon" aria-hidden>💬</span>
        <h2>Interações</h2>
      </header>

      <div className="feegow-requisition-interactions-body">
        <div className="feegow-requisition-interactions-empty">
          Nenhuma interação neste chamado.
        </div>
      </div>

      <footer className="feegow-requisition-interactions-foot">
        <input
          type="text"
          className="feegow-requisition-interactions-input"
          placeholder="Digite sua mensagem para interação no chamado..."
          value={message ?? ''}
          onChange={(e) => onSend?.(e.target.value)}
          readOnly={!onSend}
        />
        <button type="button" className="feegow-requisition-interactions-send" disabled={!onSend}>
          Enviar
        </button>
      </footer>
    </aside>
  );
}
