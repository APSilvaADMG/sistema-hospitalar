# Bula SDK
## SDK oficial do **Bula.com.br** para busca programática de medicamentos e acesso aos **links oficiais de bulas**.
🔗 https://bula.com.br

## O que é o Bula SDK
O **Bula SDK** é uma biblioteca Node.js que permite buscar medicamentos no **Bula.com.br** a partir de uma string de texto, retornando **exclusivamente URLs oficiais** das páginas de bulas.

Este SDK **não retorna conteúdo de bula**, **não realiza scraping** e **não redistribui textos protegidos**, funcionando apenas como um mecanismo de busca e redirecionamento para a fonte oficial.

## Principais características

🔎 Busca por nome comercial ou princípio ativo
🔗 Retorno de URLs canônicas do **Bula.com.br**
📦 SDK leve e fácil de integrar
⚖️ Totalmente compatível com direitos autorais
🚫 Sem scraping ou crawling

## Instalação
```bash
npm install bula-sdk
```

## Exemplo de uso
```javascript
import { searchBula } from 'bula-sdk';

async function exemplo() {
  const resultados = await searchBula('dipirona');
  console.log(resultados);
}

exemplo();
```
