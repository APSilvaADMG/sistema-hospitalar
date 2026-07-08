import { useEffect, useMemo, useState } from 'react';

import {

  api,

  type HealthInsuranceDto,

  type MedicationInsuranceMappingDto,

  type ProductDto,

} from '../api/client';

import { useAuth } from '../auth/AuthContext';

import {

  FeegowMedicationInsuranceList,

  type MedicationInsuranceFormState,

} from '../components/feegow/inventory/FeegowMedicationInsuranceList';

import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';



const PAGE_SIZE = 50;



export function FeegowMedicationInsurancePage() {

  const { hasPermission } = useAuth();

  const [mappings, setMappings] = useState<MedicationInsuranceMappingDto[]>([]);

  const [products, setProducts] = useState<ProductDto[]>([]);

  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);

  const [loading, setLoading] = useState(true);

  const [saving, setSaving] = useState(false);

  const [error, setError] = useState('');

  const [page, setPage] = useState(1);



  async function load() {

    setLoading(true);

    setError('');

    try {

      const [mappingList, productList, insuranceList] = await Promise.all([

        api.getMedicationInsuranceMappings(),

        api.getProducts('', false, 1),

        api.getHealthInsurances(),

      ]);

      setMappings(mappingList);

      setProducts(productList);

      setInsurances(insuranceList);

      setPage(1);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar cadastros.');

    } finally {

      setLoading(false);

    }

  }



  useEffect(() => {

    load().catch(console.error);

  }, []);



  const pagedMappings = useMemo(() => {

    const start = (page - 1) * PAGE_SIZE;

    return mappings.slice(start, start + PAGE_SIZE);

  }, [mappings, page]);



  async function handleCreate(payload: MedicationInsuranceFormState) {

    setSaving(true);

    try {

      await api.createMedicationInsuranceMapping({

        prescribedProductId: payload.prescribedProductId,

        referenceProductId: payload.referenceProductId,

        healthInsuranceId: payload.healthInsuranceId,

      });

      await load();

    } finally {

      setSaving(false);

    }

  }



  async function handleUpdate(id: string, payload: MedicationInsuranceFormState) {

    setSaving(true);

    try {

      await api.updateMedicationInsuranceMapping(id, {

        prescribedProductId: payload.prescribedProductId,

        referenceProductId: payload.referenceProductId,

        healthInsuranceId: payload.healthInsuranceId,

      });

      await load();

    } finally {

      setSaving(false);

    }

  }



  async function handleDelete(id: string) {

    setError('');

    try {

      await api.deleteMedicationInsuranceMapping(id);

      await load();

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao excluir cadastro.');

      throw err;

    }

  }



  return (

    <FeegowInventoryScreenLayout error={error}>

      <FeegowMedicationInsuranceList

        mappings={pagedMappings}

        products={products}

        insurances={insurances}

        loading={loading}

        saving={saving}

        canManage={hasPermission('warehouse.manage')}

        onCreate={handleCreate}

        onUpdate={handleUpdate}

        onDelete={handleDelete}

        page={page}

        pageSize={PAGE_SIZE}

        totalCount={mappings.length}

        onPageChange={setPage}

      />

    </FeegowInventoryScreenLayout>

  );

}

