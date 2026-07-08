import http from 'k6/http';
import { check, sleep } from 'k6';
import { randomItem } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const BASE_URL = __ENV.API_URL || 'http://localhost:8080';
const EMAIL = __ENV.API_EMAIL || 'admin@hospital.local';
const PASSWORD = __ENV.API_PASSWORD || 'Admin123!';

export const options = {
  scenarios: {
    hospital_api: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: Number(__ENV.K6_VUS_WARMUP || 5) },
        { duration: '2m', target: Number(__ENV.K6_VUS_PEAK || 25) },
        { duration: '1m', target: Number(__ENV.K6_VUS_PEAK || 25) },
        { duration: '30s', target: 0 },
      ],
      gracefulRampDown: '15s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.08'],
    http_req_duration: ['p(95)<3000', 'p(99)<6000'],
    checks: ['rate>0.90'],
  },
};

function jsonHeaders(token) {
  return {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
  };
}

export function setup() {
  const health = http.get(`${BASE_URL}/health`);
  check(health, { 'health ok': (r) => r.status === 200 });

  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({ email: EMAIL, password: PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  if (loginRes.status !== 200) {
    throw new Error(`Login falhou (${loginRes.status}): ${loginRes.body}`);
  }

  const login = loginRes.json();
  if (login.requiresMfa) {
    throw new Error('Usuário exige MFA — use conta sem MFA para o teste (ex.: admin@hospital.local).');
  }

  const token = login.token;
  const patientIds = [];

  for (let page = 1; page <= 5; page += 1) {
    const listRes = http.get(
      `${BASE_URL}/api/patients?page=${page}&pageSize=50`,
      jsonHeaders(token),
    );
    if (listRes.status !== 200) {
      break;
    }
    const pageData = listRes.json();
    const items = pageData.items || pageData.Items || [];
    for (const p of items) {
      const id = p.id || p.Id;
      if (id) {
        patientIds.push(id);
      }
    }
    if (items.length < 50) {
      break;
    }
  }

  return { token, patientIds, patientCount: patientIds.length };
}

export default function (data) {
  const headers = jsonHeaders(data.token);

  const searchTerms = ['Silva', 'Maria', 'LOAD', 'Ana', 'João', ''];
  const term = randomItem(searchTerms);

  const listRes = http.get(
    `${BASE_URL}/api/patients?search=${encodeURIComponent(term)}&page=1&pageSize=20`,
    headers,
  );
  check(listRes, {
    'lista pacientes 200': (r) => r.status === 200,
  });

  const quickRes = http.get(
    `${BASE_URL}/api/patients/quick-search?search=${encodeURIComponent(term)}&take=10`,
    headers,
  );
  check(quickRes, {
    'quick-search 200': (r) => r.status === 200,
  });

  if (data.patientIds.length > 0) {
    const patientId = randomItem(data.patientIds);
    const detailRes = http.get(`${BASE_URL}/api/patients/${patientId}`, headers);
    check(detailRes, {
      'detalhe paciente 200': (r) => r.status === 200,
    });

    const pepRes = http.get(`${BASE_URL}/api/patients/${patientId}/medical-record`, headers);
    check(pepRes, {
      'prontuário 200 ou 404': (r) => r.status === 200 || r.status === 404,
    });
  }

  sleep(Number(__ENV.K6_SLEEP || 0.5));
}

export function handleSummary(data) {
  const file = __ENV.K6_SUMMARY_FILE || 'scripts/load-test/k6-summary.json';
  return {
    stdout: textSummary(data, { indent: ' ', enableColors: true }),
    [file]: JSON.stringify(data, null, 2),
  };
}

function textSummary(data, opts) {
  const lines = [
    '',
    'GTH k6 — resumo',
    `  Requisições: ${data.metrics.http_reqs?.values?.count ?? 0}`,
    `  Falhas: ${((data.metrics.http_req_failed?.values?.rate ?? 0) * 100).toFixed(2)}%`,
    `  p95: ${(data.metrics.http_req_duration?.values?.['p(95)'] ?? 0).toFixed(0)} ms`,
    '',
  ];
  return lines.join('\n');
}
