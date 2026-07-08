import { useCallback, useEffect, useState } from 'react';

import { useNavigate } from 'react-router-dom';

import { api, type PatientDto } from '../api/client';

import { useAuth } from '../auth/AuthContext';

import { FeegowPatientList } from '../components/feegow/patients/FeegowPatientList';

import { FeegowPatientListSidebar } from '../components/feegow/patients/FeegowPatientListSidebar';

import type { FeegowPatientListFilter } from '../components/feegow/patients/feegowPatientNav';

import { FeegowPatientScreenLayout } from '../components/feegow/patients/FeegowPatientScreenLayout';



const PAGE_SIZE = 50;



type Props = {

  listFilter?: FeegowPatientListFilter;

};



function resolveIsActive(filter: FeegowPatientListFilter): boolean | undefined {

  if (filter === 'inactive') return false;

  return true;

}



export function FeegowPatientListPage({ listFilter = 'active' }: Props) {

  const navigate = useNavigate();

  const { hasPermission } = useAuth();

  const [patients, setPatients] = useState<PatientDto[]>([]);

  const [loading, setLoading] = useState(true);

  const [error, setError] = useState('');

  const [success, setSuccess] = useState('');

  const filter = listFilter;

  const [chartSearch, setChartSearch] = useState('');

  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

  const [page, setPage] = useState(1);

  const [totalCount, setTotalCount] = useState(0);



  const loadPatients = useCallback(async (targetPage: number) => {

    setLoading(true);

    try {

      const searchTerm = filter === 'chart-search' ? chartSearch.trim() : '';

      const result = await api.getPatients(

        searchTerm || undefined,

        targetPage,

        PAGE_SIZE,

        resolveIsActive(filter),

      );

      setPatients(result.items);

      setTotalCount(result.totalCount);

      setPage(result.page);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar pacientes.');

    } finally {

      setLoading(false);

    }

  }, [chartSearch, filter]);



  useEffect(() => {

    setPage(1);

    setSelectedIds(new Set());

    loadPatients(1).catch(console.error);

  }, [filter]);



  useEffect(() => {

    if (filter !== 'chart-search') return;

    const timer = window.setTimeout(() => {

      setPage(1);

      setSelectedIds(new Set());

      loadPatients(1).catch(console.error);

    }, 300);

    return () => window.clearTimeout(timer);

  }, [chartSearch, filter, loadPatients]);



  function handlePageChange(nextPage: number) {

    setSelectedIds(new Set());

    loadPatients(nextPage).catch(console.error);

  }



  function toggleSelect(id: string) {

    setSelectedIds((prev) => {

      const next = new Set(prev);

      if (next.has(id)) next.delete(id);

      else next.add(id);

      return next;

    });

  }



  function toggleSelectAll() {

    if (patients.every((p) => selectedIds.has(p.id))) {

      setSelectedIds(new Set());

    } else {

      setSelectedIds(new Set(patients.map((p) => p.id)));

    }

  }



  function handleEdit(id: string) {

    navigate(`/recepcao/pacientes/${id}/dados-principais`);

  }



  function handleFinance(id: string) {

    navigate(`/financeiro?paciente=${id}`);

  }



  async function handleDeactivate(id: string) {

    if (!hasPermission('patients.create')) {

      setError('Você não tem permissão para inativar pacientes.');

      return;

    }

    const patient = patients.find((p) => p.id === id);

    if (!patient || !window.confirm(`Inativar o paciente ${patient.fullName}?`)) return;



    setError('');

    setSuccess('');

    try {

      const detail = await api.getPatient(id);

      await api.updatePatient(id, {

        fullName: detail.fullName,

        birthDate: detail.birthDate,

        gender: detail.gender,

        socialName: detail.socialName,

        email: detail.email,

        phone: detail.phone,

        mobilePhone: detail.mobilePhone,

        addressStreet: detail.addressStreet,

        addressNumber: detail.addressNumber,

        addressComplement: detail.addressComplement,

        addressNeighborhood: detail.addressNeighborhood,

        addressCity: detail.addressCity,

        addressState: detail.addressState,

        addressZipCode: detail.addressZipCode,

        motherName: detail.motherName,

        emergencyContactName: detail.emergencyContactName,

        emergencyContactPhone: detail.emergencyContactPhone,

        emergencyContactRelationship: detail.emergencyContactRelationship,

        notes: detail.notes,

        photoData: detail.photoData,

        rg: detail.rg,

        nationality: detail.nationality,

        bloodType: detail.bloodType,

        occupation: detail.occupation,

        maritalStatus: detail.maritalStatus,

        birthPlace: detail.birthPlace,

        insurances: detail.insurances?.map((i) => ({

          healthInsuranceId: i.healthInsuranceId,

          cardNumber: i.cardNumber,

          planName: i.planName,

          cardHolderName: i.cardHolderName,

          productCode: i.productCode,

          cnsNumber: i.cnsNumber,

          accommodationType: i.accommodationType,

          validFrom: i.validFrom,

          validUntil: i.validUntil,

          isPrimary: i.isPrimary,

        })),

        isActive: false,

      });

      setSuccess('Paciente inativado.');

      await loadPatients(page);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao inativar paciente.');

    }

  }



  return (

    <FeegowPatientScreenLayout

      error={error}

      success={success}

      sidebar={(

        <FeegowPatientListSidebar

          filter={filter}

          chartSearch={chartSearch}

          onChartSearchChange={setChartSearch}

        />

      )}

    >

      <FeegowPatientList

        patients={patients}

        chartNumberOffset={(page - 1) * PAGE_SIZE}

        selectedIds={selectedIds}

        onToggleSelect={toggleSelect}

        onToggleSelectAll={toggleSelectAll}

        onEdit={handleEdit}

        onFinance={handleFinance}

        onDeactivate={handleDeactivate}

        loading={loading}

        page={page}

        pageSize={PAGE_SIZE}

        totalCount={totalCount}

        onPageChange={handlePageChange}

      />

    </FeegowPatientScreenLayout>

  );

}

