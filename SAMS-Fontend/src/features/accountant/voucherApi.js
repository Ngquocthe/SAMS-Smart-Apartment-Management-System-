import api from "../../lib/apiClient";

const formatDateOnly = (value) => {
  if (!value) return undefined;
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return undefined;
  return date.toISOString().split("T")[0];
};

const safeNumber = (value) => {
  const num = Number(value);
  return Number.isNaN(num) ? 0 : num;
};

const normalizeItem = (item) => {
  if (!item) return item;
  const quantity = safeNumber(item.quantity ?? item.qty ?? 1);
  const unitPrice =
    safeNumber(item.unitPrice ?? (quantity ? item.amount / quantity : 0)) || 0;
  const amount =
    safeNumber(item.amount) || (quantity && unitPrice ? quantity * unitPrice : 0);
  return {
    ...item,
    voucherItemId: item.voucherItemId || item.id,
    serviceTypeName: item.serviceTypeName || item.description || "",
    description: item.description ?? item.serviceTypeName ?? "",
    quantity,
    unitPrice,
    amount,
  };
};

const normalizeVoucher = (voucher) => {
  if (!voucher) return voucher;
  return {
    ...voucher,
    id: voucher.id ?? voucher.voucherId,
    number: voucher.number ?? voucher.voucherNo ?? voucher.voucherNumber,
    date: voucher.date ?? voucher.voucherDate,
    totalAmount: voucher.totalAmount ?? voucher.amount,
    items: Array.isArray(voucher.items)
      ? voucher.items.map(normalizeItem)
      : Array.isArray(voucher.Items)
      ? voucher.Items.map(normalizeItem)
      : [],
  };
};

const normalizePaged = (data) => {
  if (!data) {
    return {
      items: [],
      totalItems: 0,
      page: 1,
      pageSize: 20,
      totalPages: 1,
    };
  }
  const rawItems = data.items ?? data.Items ?? [];
  const items = Array.isArray(rawItems) ? rawItems.map(normalizeVoucher) : [];
  return {
    items,
    totalItems: data.totalItems ?? data.TotalItems ?? items.length,
    page: data.page ?? data.pageNumber ?? data.PageNumber ?? 1,
    pageSize: data.pageSize ?? data.PageSize ?? data.page_size ?? 20,
    totalPages:
      data.totalPages ?? data.TotalPages ?? data.total_pages ?? 1,
  };
};

const buildItemDto = (item) => {
  const quantity = safeNumber(item.quantity ?? 1);
  const unitPrice = safeNumber(item.unitPrice ?? item.amount ?? 0);
  const amount = safeNumber(item.amount);
  return {
    description: item.description?.trim() || null,
    quantity: quantity > 0 ? quantity : 1,
    unitPrice: unitPrice > 0 ? unitPrice : 0,
    amount: amount > 0 ? amount : quantity * unitPrice,
    serviceTypeId: item.serviceTypeId ?? null,
    apartmentId: item.apartmentId ?? null,
    ticketId: item.ticketId ?? null,
  };
};

export async function listVouchers(params = {}) {
  const res = await api.get("/Voucher", { params });
  return normalizePaged(res.data);
}

export async function getVoucherById(id) {
  const res = await api.get(`/Voucher/${id}`);
  return normalizeVoucher(res.data);
}

export async function createVoucher(payload = {}) {
  const dto = {
    voucherNumber: payload.voucherNumber?.trim() || null,
    date: formatDateOnly(payload.date ?? payload.voucherDate),
    companyInfo: payload.companyInfo?.trim() || null,
    totalAmount: safeNumber(payload.totalAmount ?? payload.amount ?? 0),
    description: payload.description?.trim() ?? null,
    status: payload.status ?? "DRAFT",
  };
  const res = await api.post("/Voucher", dto);
  return normalizeVoucher(res.data);
}

export async function updateVoucher(id, payload = {}) {
  const dto = {
    date: formatDateOnly(payload.date ?? payload.voucherDate),
    companyInfo: payload.companyInfo?.trim() ?? null,
    description: payload.description?.trim() ?? null,
  };
  const res = await api.put(`/Voucher/${id}`, dto);
  return normalizeVoucher(res.data);
}

export async function updateVoucherStatus(id, payload = {}) {
  const res = await api.patch(`/Voucher/${id}/status`, { status: payload.status });
  return normalizeVoucher(res.data);
}

export async function deleteVoucher(id) {
  await api.delete(`/Voucher/${id}`);
}

export async function getVoucherItems(voucherId) {
  const res = await api.get(`/VoucherItem/voucher/${voucherId}`);
  const data = Array.isArray(res.data) ? res.data : [];
  return data.map(normalizeItem);
}

export async function createVoucherFromMaintenance(payload) {
  const res = await api.post("/Voucher/from-maintenance", payload);
  return normalizeVoucher(res.data);
}

export async function checkVoucherByHistory(historyId) {
  const res = await api.get(`/Voucher/by-history/${historyId}`);
  return res.data;
}

export async function approveVoucher(id, payload = {}) {
  return updateVoucherStatus(id, { status: "APPROVED", ...payload });
}

export async function updateVoucherItem(itemId, payload = {}) {
  const dto = buildItemDto(payload);
  const res = await api.put(`/Voucher/item/${itemId}`, dto);
  return normalizeItem(res.data);
}

export async function createVoucherItem(voucherId, payload = {}) {
  const dto = {
    ...buildItemDto(payload),
    voucherId,
  };
  const res = await api.post("/VoucherItem", dto);
  return normalizeItem(res.data);
}

export async function deleteVoucherItem(itemId) {
  await api.delete(`/VoucherItem/${itemId}`);
}

export async function listVoucherItems(params = {}) {
  const res = await api.get("/VoucherItem", { params });
  const data = res.data ?? {};
  const rawItems = data.Items ?? data.items ?? data;
  const items = Array.isArray(rawItems) ? rawItems.map(normalizeItem) : [];
  return {
    items,
    totalItems: data.totalItems ?? data.TotalItems ?? items.length,
    page: data.page ?? data.pageNumber ?? data.PageNumber ?? 1,
    pageSize: data.pageSize ?? data.PageSize ?? data.page_size ?? 20,
    totalPages:
      data.totalPages ?? data.TotalPages ?? data.total_pages ?? 1,
  };
}

