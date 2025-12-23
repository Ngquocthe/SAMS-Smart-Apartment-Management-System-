export default function ServiceTypeTable({ data = [], onEdit, onDelete, onToggleActive, onOpenPrices }) {
  if (!data.length) return <div className="text-sm text-gray-500">Không có dữ liệu</div>;

  return (
    <div className="overflow-x-auto rounded-2xl shadow">
      <link 
        rel="stylesheet" 
        href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"
      />
      
      <style>{`
        .toggle-switch {
          position: relative;
          display: inline-block;
          width: 48px;
          height: 24px;
        }
        
        .toggle-switch input {
          opacity: 0;
          width: 0;
          height: 0;
        }
        
        .toggle-slider {
          position: absolute;
          cursor: pointer;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          background-color: #cbd5e1;
          transition: 0.3s;
          border-radius: 24px;
        }
        
        .toggle-slider:before {
          position: absolute;
          content: "";
          height: 18px;
          width: 18px;
          left: 3px;
          bottom: 3px;
          background-color: white;
          transition: 0.3s;
          border-radius: 50%;
        }
        
        input:checked + .toggle-slider {
          background-color: #10b981;
        }
        
        input:checked + .toggle-slider:before {
          transform: translateX(24px);
        }
        
        .action-btn {
          width: 32px;
          height: 32px;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          border-radius: 8px;
          transition: all 0.2s;
          border: none;
          cursor: pointer;
        }
        
        .action-btn:hover {
          transform: translateY(-1px);
        }
        
        .btn-edit {
          background-color: #3b82f6;
          color: white;
        }
        
        .btn-edit:hover {
          background-color: #2563eb;
        }
        
        .btn-prices {
          background-color: #8b5cf6;
          color: white;
        }
        
        .btn-prices:hover {
          background-color: #7c3aed;
        }
        
        .btn-delete {
          background-color: #ef4444;
          color: white;
        }
        
        .btn-delete:hover {
          background-color: #dc2626;
        }
      `}</style>
      
      <table className="min-w-full text-sm">
        <thead className="bg-gray-100">
          <tr>
            <th className="px-4 py-3 text-left font-semibold text-gray-700">Mã</th>
            <th className="px-4 py-3 text-left font-semibold text-gray-700">Tên</th>
            <th className="px-4 py-3 text-left font-semibold text-gray-700">Nhóm</th>
            <th className="px-4 py-3 text-center font-semibold text-gray-700">Trạng thái</th>
            <th className="px-4 py-3 text-center font-semibold text-gray-700">Thao tác</th>
          </tr>
        </thead>
        <tbody className="bg-white">
          {data.map((x) => (
            <tr key={x.serviceTypeId} className="border-t hover:bg-gray-50 transition-colors">
              <td className="px-4 py-3 font-mono text-gray-900">{x.code}</td>
              <td className="px-4 py-3 text-gray-900">{x.name}</td>
              <td className="px-4 py-3 text-gray-600">{x.categoryName || "-"}</td>
              <td className="px-4 py-3 text-center">
                <label className="toggle-switch" title={x.isActive ? "Ngưng kích hoạt" : "Kích hoạt"}>
                  <input
                    type="checkbox"
                    checked={x.isActive}
                    onChange={() => onToggleActive?.(x)}
                  />
                  <span className="toggle-slider"></span>
                </label>
              </td>
              <td className="px-4 py-3">
                <div className="flex gap-2 justify-center">
                  <button
                    onClick={() => onEdit?.(x)}
                    className="action-btn btn-edit"
                    title="Chỉnh sửa"
                  >
                    <i className="fas fa-pen"></i>
                  </button>

                  <button
                    onClick={() => onOpenPrices?.(x)}
                    className="action-btn btn-prices"
                    title="Quản lý giá"
                  >
                    <i className="fas fa-dollar-sign"></i>
                  </button>

                  <button
                    onClick={() => onDelete?.(x)}
                    className="action-btn btn-delete"
                    title="Xoá"
                  >
                    <i className="fas fa-trash"></i>
                  </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}