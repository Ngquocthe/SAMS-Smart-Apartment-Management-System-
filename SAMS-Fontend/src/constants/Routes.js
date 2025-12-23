// src/constants/Routes.js (giữ tên file bạn đang dùng)
const ROUTER_PAGE = {
  HOME: "/",
  LOGIN: "/login",
  SIGNUP: "/signup",
  ABOUT_US: "/aboutUs",
  CALLBACK: "/auth/callback",
  //USER
  USER: {
    BASE: "/user/*",
    PROFILE: "/user/profile",
  },

  PAYMENT: {
    SUCCESS: "/payment/success",
    CANCEL: "/payment/cancel",
  },
  // Admin Routes
  ADMIN: {
    BASE: "/admin/*",
    DASHBOARD: "/admin",
    USERS: "/admin/users",
    BUILDINGS: "/admin/buildings",
    REPORTS: "/admin/reports",
    SETTINGS: "/admin/settings",
    STAFF: {
      LIST_STAFF: "/admin/staff/list",
      CREATE_STAFF: "/admin/staff/create",
      EDIT_STAFF: "/admin/staff/:id/edit",
      VIEW_STAFF: "/admin/staff/:id/view",
    },
    BUILDING: {
      LIST_BUILDINGS: "/admin/building/list",
      CREATE_BUILDING: "/admin/building/create",
    },
  },

  // Building Manager Routes
  BUILDING_MANAGER: {
    BASE: "/buildingmanagement/*",
    DASHBOARD: "/buildingmanagement/dashboard",
    BUILDINGS: "/buildingmanagement/buildings",
    FLOORS: "/buildingmanagement/floors",
    APARTMENTS: "/buildingmanagement/apartments",
    AMENITIES: "/buildingmanagement/amenities",
    DOCUMENTS: "/buildingmanagement/documents",
    CARDS: "/buildingmanagement/cards",
    ASSETS: "/buildingmanagement/assets",
    MAINTENANCE_SCHEDULES: "/buildingmanagement/maintenance-schedules",
    MAINTENANCE_HISTORY: "/buildingmanagement/maintenance-history",
    FEES: "/buildingmanagement/fees",
    ANNOUNCEMENTS: "/buildingmanagement/announcements",
    REPORTS: "/buildingmanagement/reports",
    SETTINGS: "/buildingmanagement/settings",
    RESIDENTS: {
      LIST_RESIDENT: "/buildingmanagement/residents/list",
      CREATE_RESIDENT: "/buildingmanagement/residents/create",
      EDIT_RESIDENT: "/buildingmanagement/residents/:id/edit",
      VIEW_RESIDENT: "/buildingmanagement/residents/:id/view",
    },
  },

  // Accountant
  ACCOUNTANT: {
    BASE: "/accountant/*",
    SERVICE_TYPES: "/accountant/service-types",
    SERVICE_TYPE_PRICES: "/accountant/service-types/:id/prices",
    INVOICES: "/accountant/invoices",
    INVOICE_CREATE: "/accountant/invoices/create",
    INVOICE_DETAIL: "/accountant/invoices/:id",
    INVOICE_VIEW: "/accountant/invoices/:id/view",
    INVOICE_EDIT: "/accountant/invoices/:id/edit",
    RECEIPTS: "/accountant/receipts",
    // RECEIPT_CREATE: "/accountant/receipts/create", // Đã chuyển sang Receptionist
    RECEIPT_VIEW: "/accountant/receipts/:id",
    VOUCHERS: "/accountant/vouchers",
    VOUCHER_VIEW: "/accountant/vouchers/:id",
    VOUCHER_CREATE: "/accountant/vouchers/create",
    VOUCHER_EDIT: "/accountant/vouchers/:id/edit",
    // Journal Entry & Financial Reports
    JOURNAL_ENTRIES: "/accountant/journal-entries",
    JOURNAL_ENTRY_VIEW: "/accountant/journal-entries/:id",
    GENERAL_LEDGER: "/accountant/general-ledger",
    BALANCE_SHEET: "/accountant/balance-sheet",
    INCOME_STATEMENT: "/accountant/income-statement",
    FINANCIAL_DASHBOARD: "/accountant/financial-dashboard",
  },

  // Resident Routes
  RESIDENT: {
    BASE: "/resident/*",
    DASHBOARD: "/resident/dashboard",
    AMENITY_BOOKING: "/resident/amenity-booking",
    MY_BOOKINGS: "/resident/my-bookings",
    CREATE_TICKET: "/resident/create-ticket",
    MY_TICKETS: "/resident/my-tickets",
    NEWS: "/resident/news",
    DOCUMENTS: "/resident/documents",
    INVOICES: "/resident/invoices",
  },

  // Receptionist Routes
  RECEPTIONIST: {
    BASE: "/receptionist/*",
    DASHBOARD: "/receptionist/dashboard",
    DOCUMENTS: "/receptionist/documents",
    DOCUMENTS_MANAGEMENT: "/receptionist/documents-management",
    FACE_CHECK_IN: "/receptionist/face-checkin",
    RESIDENTS: "/receptionist/residents",
    REQUESTS: "/receptionist/requests",
    REPORTS: "/receptionist/reports",
    TICKETS: "/receptionist/tickets",
    TICKET_DETAIL: "/receptionist/tickets/:id",
    MAINTENANCE_SCHEDULES: "/receptionist/maintenance-schedules",
    ANNOUNCEMENTS: "/receptionist/announcements",
    RECEIPT_CREATE: "/receptionist/receipt-create",
  },
};
export default ROUTER_PAGE;
