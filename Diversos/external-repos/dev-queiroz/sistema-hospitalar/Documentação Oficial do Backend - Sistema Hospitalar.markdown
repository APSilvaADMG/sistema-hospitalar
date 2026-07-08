# Documentação Oficial do Backend - Sistema Hospitalar

Esta documentação detalha todos os endpoints da API RESTful do **Sistema Hospitalar**, hospedada em `https://sistema-hospitalar.onrender.com`. O sistema é implementado com **Node.js**, **Express** e **PostgreSQL** (via Supabase), com autenticação JWT, segurança reforçada e plena conformidade com a LGPD.

**Versão da Documentação**: 3.2.0  
**Data**: 05 de Janeiro de 2026

---

## 1. Visão Geral do Sistema

O sistema gerencia operações hospitalares completas, incluindo autenticação de profissionais (administradores, médicos e enfermeiros), pacientes, consultas, prescrições, prontuários, triagens, unidades de saúde e uma **camada avançada de Inteligência Artificial** para suporte à decisão clínica e epidemiológica.

### Características Principais

* **Arquitetura**: RESTful com endpoints organizados por recurso (`/api/auth`, `/api/consultas`, etc.).
* **Autenticação**: JWT com papéis granulares e expiração configurável.
* **Segurança**: Validação rigorosa, rate limiting, bcrypt, criptografia de dados sensíveis e proteção contra ataques comuns.
* **LGPD**: Consentimento explícito, anonimização automática, logs de auditoria e soft delete.
* **Desempenho**: Índices otimizados, cache (Redis), operações assíncronas (BullMQ) e paginação.
* **Inteligência Artificial**: Relatórios gerados com Groq SDK utilizando **exclusivamente dados agregados e anonimizados**.
* **Hospedagem**: Render.com com autoescalamento.

---

## 2. Endpoints do Sistema

Base URL: `/api`

### 2.1 Autenticação (`/auth`)

| Método | Endpoint               | Descrição                                           | Papéis Permitidos  |
|--------|------------------------|-----------------------------------------------------|--------------------|
| POST   | /auth/register-admin   | Cria administrador principal (exige `adminSecret`)  | Nenhum (secret)    |
| POST   | /auth/login            | Login e emissão de JWT                              | Todos              |
| POST   | /auth/forgot-password  | Inicia recuperação de senha                         | Nenhum             |

### 2.2 Consultas (`/consultas`)

| Método  | Endpoint                                           | Descrição                      | Papéis Permitidos   |
|---------|----------------------------------------------------|--------------------------------|---------------------|
| POST    | /consultas                                         | Cria consulta                  | MÉDICO              |
| GET     | /consultas/:id                                     | Detalhes de consulta           | MÉDICO, ENFERMEIRO  |
| GET     | /consultas/pacientes/:pacienteId                   | Consultas por paciente         | MÉDICO, ENFERMEIRO  |
| GET     | /consultas/profissional/:medicoId                  | Consultas por médico           | ADMINISTRADOR       |
| GET     | /consultas/unidade/:unidadeId                      | Todas as consultas da unidade  | ADMINISTRADOR       |
| GET     | /consultas/unidade/:unidadeId/atendimentos-ativos  | Atendimentos ativos            | ADMINISTRADOR       |
| PUT     | /consultas/:id                                     | Atualiza consulta              | MÉDICO (criador)    |
| DELETE  | /consultas/:id                                     | Soft delete                    | MÉDICO (criador)    |

### 2.3 Enfermeiros e Médicos (`/enfermeiros`, `/medicos`)

Endpoints equivalentes para ambos os recursos:

* `POST` – Criação (ADMINISTRADOR)
* `GET /` – Lista ativos
* `GET /:id` – Detalhes (ADMINISTRADOR ou próprio profissional)
* `PUT /:id` – Atualização (ADMINISTRADOR)
* `DELETE /:id` – Soft delete (ADMINISTRADOR)

### 2.4 Pacientes (`/pacientes`)

| Método  | Endpoint                  | Descrição                   | Papéis Permitidos          |
|---------|---------------------------|-----------------------------|----------------------------|
| POST    | /pacientes                | Cria paciente               | ENFERMEIRO, ADMINISTRADOR  |
| GET     | /pacientes                | Lista pacientes             | ENFERMEIRO, MÉDICO, ADMIN  |
| GET     | /pacientes/:id            | Detalhes do paciente        | ENFERMEIRO, MÉDICO, ADMIN  |
| GET     | /pacientes/:id/historico  | Histórico clínico completo  | ENFERMEIRO, MÉDICO, ADMIN  |
| PUT     | /pacientes/:id            | Atualiza paciente           | ENFERMEIRO, ADMINISTRADOR  |
| DELETE  | /pacientes/:id            | Soft delete                 | ENFERMEIRO, ADMINISTRADOR  |

### 2.5 Prescrições (`/prescricoes`)

| Método  | Endpoint                            | Descrição                 | Papéis Permitidos          |
|---------|-------------------------------------|---------------------------|----------------------------|
| POST    | /prescricoes                        | Cria prescrição           | MÉDICO, ENFERMEIRO (UPA)   |
| GET     | /prescricoes/:id                    | Detalhes                  | MÉDICO, ENFERMEIRO         |
| GET     | /prescricoes/pacientes/:pacienteId  | Prescrições por paciente  | MÉDICO, ENFERMEIRO         |
| PUT     | /prescricoes/:id                    | Atualiza                  | MÉDICO, ENFERMEIRO (UPA)   |
| DELETE  | /prescricoes/:id                    | Soft delete               | MÉDICO                     |
| GET     | /prescricoes/:id/pdf                | Gera PDF (anonimizado)    | MÉDICO, ENFERMEIRO, ADMIN  |

### 2.6 Prontuários (`/prontuarios`)

Estrutura equivalente às prescrições, com anonimização obrigatória e geração de PDF.

### 2.7 Triagens (`/triagens`)

| Método | Endpoint                                     | Descrição                        | Papéis Permitidos    |
|--------|----------------------------------------------|----------------------------------|----------------------|
| POST   | /triagens                                    | Cria triagem                     | ENFERMEIRO           |
| GET    | /triagens/:id                                | Detalhes                         | ENFERMEIRO, MÉDICO   |
| GET    | /triagens/pacientes/:pacienteId              | Triagens por paciente            | ENFERMEIRO, MÉDICO   |
| GET    | /triagens/gravidade/:cor/unidade/:unidadeId  | Triagens por gravidade e unidade | ENFERMEIRO, MÉDICO   |
| PUT    | /triagens/:id                                | Atualiza                         | ENFERMEIRO (criador) |
| DELETE | /triagens/:id                                | Soft delete                      | ENFERMEIRO (criador) |

### 2.8 Unidades de Saúde (`/unidades-saude`)

| Método | Endpoint                                 | Descrição           | Papéis Permitidos |
|--------|------------------------------------------|---------------------|-------------------|
| POST   | /unidades-saude                          | Cria unidade        | ADMINISTRADOR     |
| GET    | /unidades-saude                          | Lista unidades      | ADMINISTRADOR     |
| GET    | /unidades-saude/:id                      | Detalhes da unidade | ADMINISTRADOR     |
| GET    | /unidades-saude/:id/funcionarios         | Lista funcionários  | ADMINISTRADOR     |
| PUT    | /unidades-saude/:id                      | Atualiza unidade    | ADMINISTRADOR     |
| DELETE | /unidades-saude/:id                      | Soft delete         | ADMINISTRADOR     |
| POST   | /unidades-saude/:id/funcionarios/:funcId | Associa funcionário | ADMINISTRADOR     |

### 2.9 Inteligência Artificial (`/ia`) — **Novo em 3.2.0**

Todos os relatórios são gerados com **dados agregados e anonimizados** (conformidade total com LGPD), utilizando Groq SDK para processamento rápido e respostas em português brasileiro.

| Método | Endpoint                                      | Descrição                                                                                  | Papéis Permitidos             | Parâmetros                          |
|--------|-----------------------------------------------|--------------------------------------------------------------------------------------------|-------------------------------|-------------------------------------|
| GET    | /ia/surto                                     | Relatório de risco de surto respiratório (global ou por unidade)                           | ADMINISTRADOR, MÉDICO         | `?unidade_saude_id=UUID` (opcional) |
| GET    | /ia/paciente/:pacienteId/recorrente           | Análise de padrões de recorrência de um paciente específico                                 | ADMINISTRADOR, MÉDICO         | `:pacienteId` obrigatório           |
| GET    | /ia/triagens/:unidadeSaudeId                  | Análise operacional de triagens (risco de sobrecarga e distribuição por gravidade)        | ADMINISTRADOR, MÉDICO         | `:unidadeSaudeId` obrigatório       |
| GET    | /ia/relatorios                                | Lista os últimos relatórios gerados (histórico completo)                                   | ADMINISTRADOR, MÉDICO         | `?limit=N` (padrão 20)              |
| GET    | /ia/relatorios/:id                            | Detalhes de um relatório específico                                                        | ADMINISTRADOR, MÉDICO         | `:id` obrigatório                   |

**Armazenamento**: Tabela `relatorios_ia` com colunas: `id`, `tipo` (`surto_j`, `triagem_unidade`, `recorrente`), `conteudo` (JSONB), `unidade_saude_id`, `criado_por`, criado_em.
**Conformidade LGPD**: Nenhum dado pessoal é enviado ao modelo de IA — apenas agregados mensais ou por unidade.

---

## 3. Considerações Técnicas

* **Segurança**: HTTPS, rate limiting, proteção contra brute force e SQL injection, auditoria completa.
* **Desempenho**: Latência média de 100–300ms (PDF/IA ~1–4s), cache Redis, paginação e joins otimizados.
* **Escalabilidade**: Preparado para milhares de usuários simultâneos.
* **IA**: Uso responsável — apenas dados agregados, respostas em PT-BR, recomendações práticas e clínicas.
