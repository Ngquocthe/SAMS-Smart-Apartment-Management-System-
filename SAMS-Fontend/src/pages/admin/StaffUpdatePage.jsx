import { useParams, useNavigate } from "react-router-dom";
import StaffUpdateForm from "../../components/staff/StaffUpdate";
import ROUTER_PAGE from "../../constants/Routes";

export default function StaffUpdatePage() {
  const { id } = useParams();
  const navigate = useNavigate();

  const handleSuccess = () => {
    navigate(ROUTER_PAGE.ADMIN.STAFF.LIST_STAFF);
  };

  if (!id) {
    return <div>Không tìm thấy mã nhân sự</div>;
  }

  return <StaffUpdateForm staffCode={id} onSuccess={handleSuccess} />;
}
