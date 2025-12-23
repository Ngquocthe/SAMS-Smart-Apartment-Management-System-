import { lazy } from "react";
import ROUTER_PAGE from "../constants/Routes";
import DashboardManager from "../pages/building-manager/Dashboard";
import UserProfile from "../components/UserProfile";
import TicketDetail from "../pages/receptionist/TicketDetail";
import ReceptionistHomepage from "../pages/receptionist/ReceptionistHomepage";
import TicketList from "../pages/receptionist/TicketList";
import ReceptionistDocuments from "../pages/receptionist/Documents";
import DocumentManager from "../pages/receptionist/DocumentManager";
import Residents from "../pages/receptionist/Residents";
import FaceCheckIn from "../pages/receptionist/FaceCheckIn";
import CreateReceiptPageReceptionist from "../pages/receptionist/CreateReceiptPage";

// Building Manager Pages
import AnnouncementsManager from "../pages/building-manager/Announcements";
import FloorsManager from "../pages/building-manager/Floors";
import ApartmentsManager from "../pages/building-manager/Apartments";
import AmenitiesManager from "../pages/building-manager/amenity-manage/Amenities";
import DocumentApprovals from "../pages/building-manager/DocumentApprovals";
import BookingHistoryPage from "../pages/building-manager/amenity-manage/BookingHistoryPage";
import CardsManager from "../pages/building-manager/card-manager/Cards";
import AssetsManager from "../pages/building-manager/asset-manager/Assets";
import MaintenanceScheduleListPage from "../pages/building-manager/asset-manager/MaintenanceScheduleListPage";
import MaintenanceHistoryListPage from "../pages/building-manager/asset-manager/MaintenanceHistoryListPage";

// Resident Pages
import AmenityBooking from "../pages/resident/AmenityBooking";
import MyAmenityBookings from "../pages/resident/MyAmenityBookings";
import ResidentDashboard from "../pages/resident/Dashboard";
import CreateTicket from "../pages/resident/CreateTicket";
import MyTickets from "../pages/resident/MyTickets";
import News from "../pages/resident/News";
import Invoices from "../pages/resident/Invoices";
import ResidentDocuments from "../pages/resident/Documents";

import {
  DashboardIcon,
  ApartmentIcon,
  PeopleIcon,
  ServicesIcon,
  CardIcon,
  AssetIcon,
  MoneyIcon,
  BellIcon,
  StairsIcon,
} from "../icons/index";
import {
  HomeOutlined,
  UserOutlined,
  FileTextOutlined,
  CameraOutlined,
  BarChartOutlined,
  SolutionOutlined,
  TeamOutlined,
  StarOutlined,
  DollarOutlined,
  CalendarOutlined,
  HistoryOutlined,
  BankOutlined,
} from "@ant-design/icons";
import StaffDirectory from "../components/staff/StaffDirectory";
import ResidentAccountListPage from "../components/resident/ResidentAccountListPage";
import ResidentsList from "../pages/building-manager/ResidentsList";
import AccountantDocuments from "../pages/accountant/Documents";
import BuildingList from "../components/building/BuildingDirectory";
//Accountant Page
const ServiceTypesList = lazy(() =>
  import("../pages/accountant/service-types/index")
);
const ServiceTypePricesPage = lazy(() =>
  import("../pages/accountant/service-types/ServiceTypePricesPage")
);
const InvoiceListPage = lazy(() =>
  import("../pages/accountant/invoices/InvoiceListPage")
);
const CreateInvoicePage = lazy(() =>
  import("../pages/accountant/invoices/CreateInvoicePage")
);
const UpdateInvoicePage = lazy(() =>
  import("../pages/accountant/invoices/UpdateInvoicePage")
);
const ViewInvoicePage = lazy(() =>
  import("../pages/accountant/invoices/ViewInvoicePage")
);
const ReceiptListPage = lazy(() =>
  import("../pages/accountant/receipts/ReceiptListPage")
);
// REMOVED: Thu tiền tại quầy đã chuyển sang Receptionist
// const CreateReceiptPage = lazy(() =>
//   import("../pages/accountant/receipts/CreateReceiptPage")
// );
const ViewReceiptPage = lazy(() =>
  import("../pages/accountant/receipts/ViewReceiptPage")
);
const VoucherListPage = lazy(() =>
  import("../pages/accountant/vouchers/VoucherListPage")
);
const CreateVoucherPage = lazy(() =>
  import("../pages/accountant/vouchers/CreateVoucherPage")
);
const UpdateVoucherPage = lazy(() =>
  import("../pages/accountant/vouchers/UpdateVoucherPage")
);
const ViewVoucherPage = lazy(() =>
  import("../pages/accountant/vouchers/ViewVoucherPage")
);
// Journal Entry & Financial Reports
const JournalEntryListPage = lazy(() =>
  import("../pages/accountant/journal-entries/JournalEntryListPage")
);
const ViewJournalEntryPage = lazy(() =>
  import("../pages/accountant/journal-entries/ViewJournalEntryPage")
);
const GeneralLedgerPage = lazy(() =>
  import("../pages/accountant/journal-entries/GeneralLedgerPage")
);
const BalanceSheetPage = lazy(() =>
  import("../pages/accountant/journal-entries/BalanceSheetPage")
);
const IncomeStatementPage = lazy(() =>
  import("../pages/accountant/journal-entries/IncomeStatementPage")
);
const FinancialDashboardPage = lazy(() =>
  import("../pages/accountant/journal-entries/FinancialDashboardPage")
);

const CONFIG_ROUTER = [
  {
    show: true,
    component: DashboardManager,
    icon: DashboardIcon,
    path: ROUTER_PAGE.BUILDING_MANAGER.DASHBOARD,
    menuName: "Dashboard",
    key: "BUILDING_MANAGER_DASHBOARD",
    private: true,
    role: ["building_admin", "global_admin"],
  },
  {
    show: true,
    component: UserProfile,
    icon: UserOutlined,
    path: ROUTER_PAGE.USER.PROFILE,
    menuName: "Thông tin cá nhân",
    key: "USER_PROFILE",
    private: true,
    role: ["resident"],
  },
  {
    show: true,
    component: BuildingList,
    icon: BankOutlined,
    path: ROUTER_PAGE.ADMIN.BUILDING.LIST_BUILDINGS,
    menuName: "Quản lý tòa nhà",
    key: "BUILDING_MANAGEMENT",
    private: true,
    role: ["global_admin"],
    extraActivePaths: [ROUTER_PAGE.ADMIN.BUILDING.CREATE_BUILDING],
  },
  {
    key: "USER_ACCOUNT_MGMT",
    menuName: "Quản lý tài khoản",
    private: true,
    role: ["building-manager", "global_admin"],
    icon: TeamOutlined,
    children: [
      {
        key: "USER_STAFFS",
        menuName: "Nhân viên",
        component: StaffDirectory,
        path: ROUTER_PAGE.ADMIN.STAFF.LIST_STAFF,
        icon: SolutionOutlined,
        private: true,
        role: ["global_admin"],
        show: true,
        extraActivePaths: [
          ROUTER_PAGE.ADMIN.STAFF.CREATE_STAFF,
          ROUTER_PAGE.ADMIN.STAFF.EDIT_STAFF,
        ],
      },
      {
        key: "USER_RESIDENTS",
        menuName: "Cư dân",
        component: ResidentsList,
        path: ROUTER_PAGE.BUILDING_MANAGER.RESIDENTS.LIST_RESIDENT,
        icon: UserOutlined,
        private: true,
        role: ["building_admin", "global_admin", "resident_manager"],
        show: true,
      },
    ],
  },
  {
    show: true,
    component: AnnouncementsManager,
    icon: BellIcon,
    path: ROUTER_PAGE.BUILDING_MANAGER.ANNOUNCEMENTS,
    menuName: "Quản lý tin tức",
    key: "ANNOUNCEMENTS",
    private: true,
    role: ["building_admin"],
  },
  {
    show: true,
    component: FloorsManager,
    icon: StairsIcon,
    path: ROUTER_PAGE.BUILDING_MANAGER.FLOORS,
    menuName: "Quản lý tầng",
    key: "FLOORS",
    private: true,
    role: ["building_admin"],
  },
  {
    show: true,
    component: ApartmentsManager,
    icon: ApartmentIcon,
    path: ROUTER_PAGE.BUILDING_MANAGER.APARTMENTS,
    menuName: "Quản lý căn hộ",
    key: "APARTMENTS",
    private: true,
    role: ["building_admin"],
  },
  {
    show: true,
    component: AmenitiesManager,
    icon: PeopleIcon,
    path: ROUTER_PAGE.BUILDING_MANAGER.AMENITIES,
    menuName: "Quản lý tiện ích",
    key: "AMENITIES",
    private: true,
    role: ["building_admin"],
  },
  {
    show: true,
    component: DocumentApprovals,
    icon: FileTextOutlined,
    path: ROUTER_PAGE.BUILDING_MANAGER.DOCUMENTS,
    menuName: "Quản lý tài liệu",
    key: "BUILDING_MANAGER_DOCUMENTS",
    private: true,
    role: ["building_admin"],
  },
  {
    show: false,
    component: BookingHistoryPage,
    path: "/buildingmanagement/amenities/booking-history",
    key: "AMENITIES_BOOKING_HISTORY",
    private: true,
    role: ["building_admin"],
  },
  {
    show: true,
    component: CardsManager,
    icon: CardIcon,
    path: ROUTER_PAGE.BUILDING_MANAGER.CARDS,
    menuName: "Quản lý thẻ",
    key: "CARDS",
    private: true,
    role: ["building_admin"],
  },
  {
    show: true,
    component: AssetsManager,
    icon: AssetIcon,
    path: ROUTER_PAGE.BUILDING_MANAGER.ASSETS,
    menuName: "Quản lý tài sản",
    key: "ASSETS",
    private: true,
    role: ["building_admin"],
  },
  {
    show: true,
    component: MaintenanceScheduleListPage,
    icon: CalendarOutlined,
    path: ROUTER_PAGE.BUILDING_MANAGER.MAINTENANCE_SCHEDULES,
    menuName: " Quản lý lịch bảo trì tài sản",
    key: "MAINTENANCE_SCHEDULES",
    private: true,
    role: ["building_admin"],
  },
  {
    show: true,
    component: MaintenanceHistoryListPage,
    icon: HistoryOutlined,
    path: ROUTER_PAGE.BUILDING_MANAGER.MAINTENANCE_HISTORY,
    menuName: "Lịch sử bảo trì tài sản ",
    key: "MAINTENANCE_HISTORY",
    private: true,
    role: ["building_admin"],
  },
  // Receptionist routes
  {
    key: "RECEPTIONIST_DASHBOARD",
    path: ROUTER_PAGE.RECEPTIONIST.DASHBOARD,
    component: ReceptionistHomepage,
    menuName: "Trang chủ",
    icon: HomeOutlined,
    role: ["receptionist"],
    show: true,
    private: true,
  },
  {
    key: "RECEPTIONIST_DOCUMENTS",
    path: ROUTER_PAGE.RECEPTIONIST.DOCUMENTS,
    component: ReceptionistDocuments,
    menuName: "Tài liệu của lễ tân",
    icon: FileTextOutlined,
    role: ["receptionist"],
    show: true,
    private: true,
  },

  {
    key: "RECEPTIONIST_FACE_CHECKIN",
    path: ROUTER_PAGE.RECEPTIONIST.FACE_CHECK_IN,
    component: FaceCheckIn,
    menuName: "Điểm danh khuôn mặt",
    icon: CameraOutlined,
    role: ["receptionist"],
    show: true,
    private: true,
  },
  {
    key: "RECEPTIONIST_DOCUMENTS_MANAGEMENT",
    path: ROUTER_PAGE.RECEPTIONIST.DOCUMENTS_MANAGEMENT,
    component: DocumentManager,
    menuName: "Quản lý tài liệu",
    icon: FileTextOutlined,
    role: ["receptionist"],
    show: true,
    private: true,
  },
  {
    key: "RECEPTIONIST_TICKETS",
    path: ROUTER_PAGE.RECEPTIONIST.TICKETS,
    component: TicketList,
    menuName: "Yêu cầu",
    icon: FileTextOutlined,
    role: ["receptionist"],
    show: true,
    private: true,
  },
  {
    key: "RECEPTIONIST_TICKET_DETAIL",
    path: ROUTER_PAGE.RECEPTIONIST.TICKET_DETAIL,
    component: TicketDetail,
    role: ["receptionist"],
    show: false,
    private: true,
  },
  {
    key: "RECEPTIONIST_RESIDENTS",
    path: ROUTER_PAGE.RECEPTIONIST.RESIDENTS,
    component: Residents,
    menuName: "Cư dân",
    icon: UserOutlined,
    role: ["receptionist"],
    show: true,
    private: true,
  },
  {
    key: "RECEPTIONIST_ANNOUNCEMENTS",
    path: ROUTER_PAGE.RECEPTIONIST.ANNOUNCEMENTS,
    component: AnnouncementsManager,
    menuName: "Quản lý tin tức",
    icon: BellIcon,
    role: ["receptionist"],
    show: true,
    private: true,
  },
  {
    key: "RECEPTIONIST_MAINTENANCE_SCHEDULES",
    path: ROUTER_PAGE.RECEPTIONIST.MAINTENANCE_SCHEDULES,
    component: MaintenanceScheduleListPage,
    icon: AssetIcon,
    menuName: "Quản lý lịch bảo trì",
    role: ["receptionist"],
    show: true,
    private: true,
  },
  {
    key: "RECEPTIONIST_RECEIPT_CREATE",
    path: ROUTER_PAGE.RECEPTIONIST.RECEIPT_CREATE,
    component: CreateReceiptPageReceptionist,
    icon: DollarOutlined,
    menuName: "Thu tiền tại quầy",
    role: ["receptionist"],
    show: true,
    private: true,
  },

  // Resident routes
  {
    key: "RESIDENT_DASHBOARD",
    path: ROUTER_PAGE.RESIDENT.DASHBOARD,
    component: ResidentDashboard,
    menuName: "Dashboard",
    icon: HomeOutlined,
    role: ["resident"],
    show: true,
    private: true,
  },
  {
    key: "RESIDENT_NEWS",
    path: ROUTER_PAGE.RESIDENT.NEWS,
    component: News,
    menuName: "Tin tức",
    icon: BellIcon,
    role: ["resident"],
    show: true,
    private: true,
  },
  {
    key: "RESIDENT_DOCUMENTS",
    path: ROUTER_PAGE.RESIDENT.DOCUMENTS,
    component: ResidentDocuments,
    menuName: "Tài liệu",
    icon: FileTextOutlined,
    role: ["resident"],
    show: true,
    private: true,
  },
  {
    key: "RESIDENT_CREATE_TICKET",
    path: ROUTER_PAGE.RESIDENT.CREATE_TICKET,
    component: CreateTicket,
    menuName: "Tạo yêu cầu",
    icon: FileTextOutlined,
    role: ["resident"],
    show: true,
    private: true,
  },
  {
    key: "RESIDENT_MY_TICKETS",
    path: ROUTER_PAGE.RESIDENT.MY_TICKETS,
    component: MyTickets,
    menuName: "Yêu cầu của tôi",
    icon: FileTextOutlined,
    role: ["resident"],
    show: true,
    private: true,
  },
  {
    key: "RESIDENT_AMENITY_BOOKING",
    path: ROUTER_PAGE.RESIDENT.AMENITY_BOOKING,
    component: AmenityBooking,
    menuName: "Đăng ký tiện ích",
    icon: StarOutlined,
    role: ["resident"],
    show: true,
    private: true,
  },
  {
    key: "RESIDENT_MY_BOOKINGS",
    path: ROUTER_PAGE.RESIDENT.MY_BOOKINGS,
    component: MyAmenityBookings,
    menuName: "Lịch sử đăng ký",
    icon: FileTextOutlined,
    role: ["resident"],
    show: true,
    private: true,
  },
  {
    key: "RESIDENT_INVOICES",
    path: ROUTER_PAGE.RESIDENT.INVOICES,
    component: Invoices,
    menuName: "Hóa đơn",
    icon: MoneyIcon,
    role: ["resident"],
    show: true,
    private: true,
  },

  {
    key: "SECURITY_DASHBOARD",
    path: "/security/dashboard",
    menuName: "Dashboard",
    icon: HomeOutlined,
    role: ["security"],
    show: true,
    private: true,
  },
  {
    key: "SECURITY_VISITORS",
    path: "/security/visitors",
    menuName: "Visitors",
    icon: UserOutlined,
    role: ["security"],
    show: true,
    private: true,
  },
  //Accountant routes
  {
    show: true,
    component: ServiceTypesList,
    icon: ServicesIcon,
    path: ROUTER_PAGE.ACCOUNTANT.SERVICE_TYPES,
    menuName: "Loại dịch vụ",
    key: "SERVICE_TYPES",
    private: true,
    role: ["accountant"],
  },
  {
    show: false,
    component: ServiceTypePricesPage,
    path: ROUTER_PAGE.ACCOUNTANT.SERVICE_TYPE_PRICES,
    key: "SERVICE_TYPE_PRICES",
    private: true,
    role: ["accountant"],
  },
  {
    show: true,
    component: InvoiceListPage,
    icon: FileTextOutlined,
    path: ROUTER_PAGE.ACCOUNTANT.INVOICES,
    menuName: "Hoá đơn",
    key: "ACCOUNTANT_INVOICES",
    private: true,
    role: ["accountant"],
  },
  {
    show: false,
    component: CreateInvoicePage,
    path: ROUTER_PAGE.ACCOUNTANT.INVOICE_CREATE,
    key: "ACCOUNTANT_INVOICE_CREATE",
    private: true,
    role: ["accountant"],
  },
  {
    show: false,
    component: UpdateInvoicePage,
    path: ROUTER_PAGE.ACCOUNTANT.INVOICE_DETAIL,
    key: "ACCOUNTANT_INVOICE_DETAIL",
    private: true,
    role: ["accountant"],
  },
  {
    show: false,
    component: ViewInvoicePage,
    path: ROUTER_PAGE.ACCOUNTANT.INVOICE_VIEW,
    key: "ACCOUNTANT_INVOICE_VIEW",
    private: true,
    role: ["accountant"],
  },
  {
    show: false,
    component: UpdateInvoicePage,
    path: ROUTER_PAGE.ACCOUNTANT.INVOICE_EDIT,
    key: "ACCOUNTANT_INVOICE_EDIT",
    private: true,
    role: ["accountant"],
  },
  {
    show: true,
    component: ReceiptListPage,
    icon: DollarOutlined,
    path: ROUTER_PAGE.ACCOUNTANT.RECEIPTS,
    menuName: "Biên lai",
    key: "ACCOUNTANT_RECEIPTS",
    private: true,
    role: "accountant",
  },

  // REMOVED: Thu tiền tại quầy đã chuyển sang Receptionist
  // {
  //   show: false,
  //   component: CreateReceiptPage,
  //   path: ROUTER_PAGE.ACCOUNTANT.RECEIPT_CREATE,
  //   key: "ACCOUNTANT_RECEIPT_CREATE",
  //   private: true,
  //   role: "accountant",
  // },
  {
    show: false,
    component: ViewReceiptPage,
    path: ROUTER_PAGE.ACCOUNTANT.RECEIPT_VIEW,
    key: "ACCOUNTANT_RECEIPT_VIEW",
    private: true,
    role: "accountant",
  },
  {
    show: true,
    component: VoucherListPage,
    icon: FileTextOutlined,
    path: ROUTER_PAGE.ACCOUNTANT.VOUCHERS,
    menuName: "Phiếu chi",
    key: "ACCOUNTANT_VOUCHERS",
    private: true,
    role: "accountant",
  },
  {
    show: false,
    component: CreateVoucherPage,
    path: ROUTER_PAGE.ACCOUNTANT.VOUCHER_CREATE,
    key: "ACCOUNTANT_VOUCHER_CREATE",
    private: true,
    role: "accountant",
  },
  {
    show: false,
    component: UpdateVoucherPage,
    path: ROUTER_PAGE.ACCOUNTANT.VOUCHER_EDIT,
    key: "ACCOUNTANT_VOUCHER_EDIT",
    private: true,
    role: "accountant",
  },
  {
    show: false,
    component: ViewVoucherPage,
    path: ROUTER_PAGE.ACCOUNTANT.VOUCHER_VIEW,
    key: "ACCOUNTANT_VOUCHER_VIEW",
    private: true,
    role: "accountant",
  },
  // Journal Entry Routes
  {
    show: true,
    component: JournalEntryListPage,
    icon: FileTextOutlined,
    path: ROUTER_PAGE.ACCOUNTANT.JOURNAL_ENTRIES,
    menuName: "Sổ nhật ký",
    key: "ACCOUNTANT_JOURNAL_ENTRIES",
    private: true,
    role: "accountant",
  },
  {
    show: false,
    component: ViewJournalEntryPage,
    path: ROUTER_PAGE.ACCOUNTANT.JOURNAL_ENTRY_VIEW,
    key: "ACCOUNTANT_JOURNAL_ENTRY_VIEW",
    private: true,
    role: "accountant",
  },
  {
    show: false,
    component: GeneralLedgerPage,
    path: ROUTER_PAGE.ACCOUNTANT.GENERAL_LEDGER,
    key: "ACCOUNTANT_GENERAL_LEDGER",
    private: true,
    role: "accountant",
  },
  {
    show: false,
    component: BalanceSheetPage,
    path: ROUTER_PAGE.ACCOUNTANT.BALANCE_SHEET,
    key: "ACCOUNTANT_BALANCE_SHEET",
    private: true,
    role: "accountant",
  },
  {
    show: false,
    component: IncomeStatementPage,
    path: ROUTER_PAGE.ACCOUNTANT.INCOME_STATEMENT,
    key: "ACCOUNTANT_INCOME_STATEMENT",
    private: true,
    role: "accountant",
  },
  {
    show: true,
    component: FinancialDashboardPage,
    icon: BarChartOutlined,
    path: ROUTER_PAGE.ACCOUNTANT.FINANCIAL_DASHBOARD,
    menuName: "Dashboard tài chính",
    key: "ACCOUNTANT_FINANCIAL_DASHBOARD",
    private: true,
    role: "accountant",
  },
  {
    show: true,
    component: AccountantDocuments,
    icon: FileTextOutlined,
    path: "/accountant/documents",
    menuName: "Tài liệu Kế Toán",
    key: "ACCOUNTANT_DOCUMENTS",
    private: true,
    role: "accountant",
  },
];

export default CONFIG_ROUTER;
