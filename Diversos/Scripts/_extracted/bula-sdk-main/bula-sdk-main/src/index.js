import fetch from 'node-fetch';

const BASE_URL = 'https://bula.com.br/api/search';

/**
 * Busca medicamentos no Bula.com.br e retorna URLs oficiais.
 *
 * @param {string} query
 * @returns {Promise<Array<{title: string, url: string}>>}
 */
export async function searchBula(query) {
	if (!query || typeof query !== 'string') {
		throw new Error('Query inválida');
	}

	const url = `${BASE_URL}?public=true&query=${encodeURIComponent(query)}`;

	const res = await fetch(url, {
		headers: {
			'User-Agent': 'bula-sdk/1.0'
		}
	});

	if (!res.ok) {
		throw new Error('Erro ao consultar a API do Bula');
	}

	const data = await res.json();

	return data.resultados || [];
}