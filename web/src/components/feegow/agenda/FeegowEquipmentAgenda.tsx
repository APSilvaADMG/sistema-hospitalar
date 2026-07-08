import { getInstitutionShortName } from '../../../config/iasghBranding';

type Props = {
  date: string;
  equipmentId: string;
};

function formatAgendaTitle(dateStr: string) {
  const d = new Date(`${dateStr}T12:00:00`);
  const text = d.toLocaleDateString('pt-BR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
  return text.charAt(0).toUpperCase() + text.slice(1);
}

export function FeegowEquipmentAgenda({ date, equipmentId }: Props) {
  const institution = getInstitutionShortName();
  const hasEquipment = Boolean(equipmentId);
  const weekday = new Date(`${date}T12:00:00`).getDay();

  return (
    <div className="feegow-agenda-schedule feegow-equipment-agenda">
      <header className="feegow-agenda-schedule-head">
        <h1 className="feegow-agenda-schedule-title">Agenda de Equipamentos</h1>
        <p className="feegow-agenda-schedule-date">
          <span className="feegow-agenda-cal-icon" aria-hidden>📅</span>
          <span className="feegow-crumb-sep">/</span>
          {formatAgendaTitle(date)}
        </p>
      </header>

      <div className="feegow-agenda-schedule-body feegow-equipment-body">
        {!hasEquipment ? (
          <p className="feegow-agenda-empty-hint">Selecione um equipamento na barra lateral.</p>
        ) : weekday === 0 || weekday === 6 ? (
          <p className="feegow-agenda-empty-hint">Não há grade configurada para este dia da semana.</p>
        ) : (
          <div className="feegow-equipment-room">
            <div className="feegow-agenda-room-title">SALA DE EQUIPAMENTOS ({institution})</div>
            <p className="feegow-agenda-empty-hint">Não há grade configurada para este dia da semana.</p>
          </div>
        )}
      </div>
    </div>
  );
}
