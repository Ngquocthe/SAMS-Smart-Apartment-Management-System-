import api from "../../lib/apiClient";

// GET /api/JournalEntry - Danh sách sổ nhật ký chung
export async function listJournalEntries(params = {}) {
  const res = await api.get("/JournalEntry", { params });
  return res.data;
}

// GET /api/JournalEntry/{id} - Chi tiết bút toán
export async function getJournalEntryById(id) {
  const res = await api.get(`/JournalEntry/${id}`);
  return res.data;
}

// GET /api/JournalEntry/ledger - Sổ cái theo tài khoản
export async function getGeneralLedger(params = {}) {
  const backendParams = {};

  if (params.accountCode) backendParams.accountCode = params.accountCode;
  if (params.period) {
    backendParams.period = params.period;
  } else {
    if (params.dateFrom) backendParams.from = params.dateFrom;
    if (params.dateTo) backendParams.to = params.dateTo;
  }

  const res = await api.get("/JournalEntry/ledger", { params: backendParams });
  return res.data;
}

// GET /api/JournalEntry/balance-sheet - Bảng cân đối kế toán
export async function getBalanceSheet(params = {}) {
  const backendParams = {};

  if (params.period) backendParams.period = params.period;
  if (params.dateFrom && !backendParams.period) {
    backendParams.from = params.dateFrom;
  }
  if (params.dateTo && !backendParams.period) {
    backendParams.to = params.dateTo;
  }

  const res = await api.get("/JournalEntry/balance-sheet", {
    params: backendParams,
  });
  return res.data;
}

// GET /api/JournalEntry/income-statement - Báo cáo thu chi
export async function getIncomeStatement(params = {}) {
  const backendParams = {};

  if (params.dateFrom) backendParams.from = params.dateFrom;
  if (params.dateTo) backendParams.to = params.dateTo;

  const res = await api.get("/JournalEntry/income-statement", {
    params: backendParams,
  });
  return res.data;
}

// GET /api/JournalEntry/dashboard - Dashboard tài chính
export async function getFinancialDashboard(params = {}) {
  const res = await api.get("/JournalEntry/dashboard", { params });
  return res.data;
}

