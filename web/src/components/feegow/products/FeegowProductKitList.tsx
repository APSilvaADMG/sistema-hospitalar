import { Link } from 'react-router-dom';



import type { ProductKitDto } from '../../../api/client';

import { TablePagination } from '../TablePagination';



type Props = {

  kits: ProductKitDto[];

  search: string;

  onSearchChange: (value: string) => void;

  onSearch: () => void;

  onEdit: (id: string) => void;

  onDelete: (id: string) => void;

  loading?: boolean;

  canManage?: boolean;

  page?: number;

  pageSize?: number;

  totalCount?: number;

  onPageChange?: (page: number) => void;

};



export function FeegowProductKitList({

  kits,

  search,

  onSearchChange,

  onSearch,

  onEdit,

  onDelete,

  loading,

  canManage,

  page,

  pageSize,

  totalCount,

  onPageChange,

}: Props) {

  const showPagination = page != null && pageSize != null && totalCount != null && onPageChange != null;



  return (

    <div className="feegow-inventory-page">

      <header className="feegow-inventory-page-head">

        <div className="feegow-inventory-breadcrumb">

          <span>Kits de Produtos</span>

          <span className="feegow-inventory-crumb-sep">/</span>

        </div>

        {canManage ? (

          <Link to="/estoque/kits/inserir" className="feegow-inventory-insert-btn">

            + INSERIR

          </Link>

        ) : null}

      </header>



      <section className="feegow-inventory-filter-card feegow-inventory-kit-filters">

        <div className="feegow-inventory-filter-row2 feegow-inventory-kit-filter-row">

          <label className="feegow-inventory-field feegow-inventory-field-grow">

            <span>Buscar</span>

            <input

              value={search}

              onChange={(e) => onSearchChange(e.target.value)}

              onKeyDown={(e) => {

                if (e.key === 'Enter') {

                  e.preventDefault();

                  onSearch();

                }

              }}

              placeholder="Nome do kit"

            />

          </label>

          <button type="button" className="feegow-inventory-search-btn" onClick={onSearch}>

            Buscar

          </button>

        </div>

      </section>



      <section className="feegow-inventory-panel feegow-inventory-table-card">

        <div className="feegow-inventory-table-wrap">

          <table className="feegow-inventory-table">

            <thead>

              <tr>

                <th>Nome</th>

                <th>Tabela</th>

                <th>Itens</th>

                <th>Valor total</th>

                <th className="feegow-inventory-table-actions-col">Ações</th>

              </tr>

            </thead>

            <tbody>

              {kits.map((kit) => (

                <tr key={kit.id} className="feegow-inventory-table-row-static">

                  <td>{kit.name}</td>

                  <td>{kit.priceTable || '—'}</td>

                  <td>{kit.itemCount}</td>

                  <td>

                    {kit.totalUnitPrice.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}

                  </td>

                  <td className="feegow-inventory-table-actions">

                    <button type="button" onClick={() => onEdit(kit.id)}>Editar</button>

                    {canManage ? (

                      <button type="button" onClick={() => onDelete(kit.id)}>Excluir</button>

                    ) : null}

                  </td>

                </tr>

              ))}

              {!loading && kits.length === 0 ? (

                <tr>

                  <td colSpan={5} className="feegow-inventory-table-empty">

                    Nenhum kit encontrado.

                  </td>

                </tr>

              ) : null}

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

      </section>

    </div>

  );

}

