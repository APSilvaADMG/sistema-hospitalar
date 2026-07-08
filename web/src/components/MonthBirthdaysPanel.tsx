import { useMemo } from 'react';

import type { DashboardBirthdayEmployeeDto } from '../api/client';
import { PersonAvatar } from './PersonAvatar';
import { formatBrDate } from '../utils/dateUtils';

type MonthBirthdaysPanelProps = {
  birthdays: DashboardBirthdayEmployeeDto[];
};

function formatMonthLabel(date: Date) {
  const label = date.toLocaleDateString('pt-BR', { month: 'long' });
  return label.charAt(0).toUpperCase() + label.slice(1);
}

export function MonthBirthdaysPanel({ birthdays }: MonthBirthdaysPanelProps) {
  const monthLabel = useMemo(() => formatMonthLabel(new Date()), []);

  return (
    <div className="card-panel appt-panel month-birthdays-panel">
      <div className="card-panel-header">
        Aniversariantes de {monthLabel}
        {birthdays.length > 0 && (
          <span className="month-birthdays-count">{birthdays.length}</span>
        )}
      </div>
      <div className="card-panel-body">
        {birthdays.length === 0 ? (
          <p className="month-birthdays-empty">
            Nenhum funcionário com data de nascimento cadastrada neste mês.
          </p>
        ) : (
          <ul className="month-birthdays-list">
            {birthdays.map((employee) => (
              <li key={employee.id} className="month-birthday-item">
                <PersonAvatar
                  name={employee.fullName}
                  photoData={employee.photoData}
                  size={52}
                  className="month-birthday-avatar"
                />
                <div className="month-birthday-info">
                  <span className="month-birthday-name">{employee.fullName}</span>
                  <span className="month-birthday-date">
                    {formatBrDate(employee.birthDate)}
                  </span>
                  {(employee.jobTitle || employee.departmentName) && (
                    <span className="month-birthday-meta">
                      {[employee.jobTitle, employee.departmentName].filter(Boolean).join(' · ')}
                    </span>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
