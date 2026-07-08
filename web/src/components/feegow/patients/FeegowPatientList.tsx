import { Link } from 'react-router-dom';

import type { PatientDto } from '../../../api/client';

import { formatBrDate } from '../../../utils/dateUtils';

import { formatCpfInput, formatPhoneInput } from '../../../utils/inputMasks';

import { TablePagination } from '../TablePagination';

import { genderLabelFeegow } from './feegowPatientListUtils';



function formatListCpf(cpf?: string | null): string {

  if (!cpf) return '';

  return formatCpfInput(cpf);

}



function formatListPhone(phone?: string | null): string {

  if (!phone) return '';

  return formatPhoneInput(phone);

}



type Props = {

  patients: PatientDto[];

  chartNumberOffset?: number;

  selectedIds: Set<string>;

  onToggleSelect: (id: string) => void;

  onToggleSelectAll: () => void;

  onEdit: (id: string) => void;

  onFinance: (id: string) => void;

  onDeactivate: (id: string) => void;

  loading?: boolean;

  page?: number;

  pageSize?: number;

  totalCount?: number;

  onPageChange?: (page: number) => void;

};



export function FeegowPatientList({

  patients,

  chartNumberOffset = 0,

  selectedIds,

  onToggleSelect,

  onToggleSelectAll,

  onEdit,

  onFinance,

  onDeactivate,

  loading,

  page,

  pageSize,

  totalCount,

  onPageChange,

}: Props) {

  const allSelected = patients.length > 0 && patients.every((p) => selectedIds.has(p.id));

  const showPagination = page != null && pageSize != null && totalCount != null && onPageChange != null;



  return (

    <div className="feegow-patient-list-card">

      <header className="feegow-patient-list-head">

        <div className="feegow-patient-list-breadcrumb">

          <span>Pacientes</span>

          <span className="feegow-patient-list-crumb-sep">/</span>

          <span className="feegow-patient-list-crumb-user" aria-hidden>👤</span>

        </div>

        <Link to="/recepcao/pacientes/inserir" className="feegow-patient-insert-btn">

          + INSERIR

        </Link>

      </header>



      <div className="feegow-patient-list-table-wrap">

        <table className="feegow-patient-list-table">

          <thead>

            <tr>

              <th className="feegow-patient-list-check-col">

                <input

                  type="checkbox"

                  aria-label="Selecionar todos"

                  checked={allSelected}

                  onChange={onToggleSelectAll}

                />

              </th>

              <th>Prontuário</th>

              <th>Nome</th>

              <th>CPF</th>

              <th>Convênio</th>

              <th>Nascimento</th>

              <th>Sexo</th>

              <th>Telefone</th>

              <th>Celular</th>

              <th>Propostas</th>

              <th>Últ. Agend.</th>

              <th>Próx. Agend.</th>

              <th className="feegow-patient-list-actions-col" aria-label="Ações" />

            </tr>

          </thead>

          <tbody>

            {loading ? (

              <tr>

                <td colSpan={13} className="feegow-patient-list-empty">Carregando pacientes…</td>

              </tr>

            ) : patients.length === 0 ? (

              <tr>

                <td colSpan={13} className="feegow-patient-list-empty">Nenhum paciente encontrado.</td>

              </tr>

            ) : (

              patients.map((patient, index) => (

                <tr key={patient.id}>

                  <td className="feegow-patient-list-check-col">

                    <input

                      type="checkbox"

                      aria-label={`Selecionar ${patient.fullName}`}

                      checked={selectedIds.has(patient.id)}

                      onChange={() => onToggleSelect(patient.id)}

                    />

                  </td>

                  <td>{chartNumberOffset + index + 1}</td>

                  <td>

                    <Link

                      to={`/recepcao/pacientes/${patient.id}/dados-principais`}

                      className="feegow-patient-list-name"

                    >

                      {patient.fullName}

                    </Link>

                  </td>

                  <td>{formatListCpf(patient.cpf)}</td>

                  <td>{patient.primaryInsuranceName ?? ''}</td>

                  <td>{formatBrDate(patient.birthDate, '')}</td>

                  <td>{genderLabelFeegow(patient.gender)}</td>

                  <td>{formatListPhone(patient.phone)}</td>

                  <td>{formatListPhone(patient.mobilePhone)}</td>

                  <td>{patient.openReceivableCount ? String(patient.openReceivableCount) : ''}</td>

                  <td>{formatBrDate(patient.lastAppointmentAt, '')}</td>

                  <td>{formatBrDate(patient.nextAppointmentAt, '')}</td>

                  <td className="feegow-patient-list-actions-col">

                    <div className="feegow-patient-list-actions">

                      <button

                        type="button"

                        className="feegow-patient-action feegow-patient-action-edit"

                        title="Editar"

                        aria-label={`Editar ${patient.fullName}`}

                        onClick={() => onEdit(patient.id)}

                      >

                        ✎

                      </button>

                      <button

                        type="button"

                        className="feegow-patient-action feegow-patient-action-finance"

                        title="Financeiro"

                        aria-label={`Financeiro de ${patient.fullName}`}

                        onClick={() => onFinance(patient.id)}

                      >

                        $

                      </button>

                      <button

                        type="button"

                        className="feegow-patient-action feegow-patient-action-delete"

                        title="Inativar"

                        aria-label={`Inativar ${patient.fullName}`}

                        onClick={() => onDeactivate(patient.id)}

                      >

                        ✕

                      </button>

                    </div>

                  </td>

                </tr>

              ))

            )}

          </tbody>

        </table>

      </div>



      {showPagination ? (

        <TablePagination

          page={page}

          pageSize={pageSize}

          totalCount={totalCount}

          onPageChange={onPageChange}

          loading={loading}

        />

      ) : null}

    </div>

  );

}

