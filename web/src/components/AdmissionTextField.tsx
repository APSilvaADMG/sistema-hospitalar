type AdmissionTextFieldProps = {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  snippets: string[];
  required?: boolean;
  placeholder?: string;
};

export function AdmissionTextField({
  id,
  label,
  value,
  onChange,
  snippets,
  required,
  placeholder,
}: AdmissionTextFieldProps) {
  const listId = `${id}-snippets`;

  return (
    <div className="form-field full">
      <label htmlFor={id}>{label}{required ? ' *' : ''}</label>
      {snippets.length > 0 && (
        <div className="pep-template-chips" style={{ marginBottom: 8 }}>
          <span className="pep-template-label">Sugestões:</span>
          {snippets.map((snippet) => (
            <button
              key={snippet}
              type="button"
              className="pep-template-chip"
              onClick={() => onChange(snippet)}
            >
              {snippet.length > 52 ? `${snippet.slice(0, 52)}…` : snippet}
            </button>
          ))}
        </div>
      )}
      <input
        id={id}
        list={listId}
        required={required}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder ?? 'Selecione uma sugestão ou digite um novo texto'}
      />
      <datalist id={listId}>
        {snippets.map((snippet) => (
          <option key={snippet} value={snippet} />
        ))}
      </datalist>
      <p className="form-hint">Textos novos são salvos automaticamente ao confirmar a internação.</p>
    </div>
  );
}
