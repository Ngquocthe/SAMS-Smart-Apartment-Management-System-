import React from "react";
import { Input, Select, Button, Segmented, Tooltip } from "antd";
import { FilterOutlined, UndoOutlined } from "@ant-design/icons";
import { VisibilityScope } from "../../features/documents/visibilityScopes";

const SCOPE_ALL_VALUE = "__ALL__";
const scopeFilterOptions = [
    { label: "Tất cả", value: SCOPE_ALL_VALUE },
    { label: "Kế toán", value: VisibilityScope.Accounting },
    { label: "Lễ tân", value: VisibilityScope.Receptionist },
    { label: "Cư dân", value: VisibilityScope.Resident },
];

export default function DocumentFilters({
    query,
    statusOptions,
    statusValue,
    statusAllValue,
    categoryOptions,
    onKeywordChange,
    onStatusChange,
    onCategoryChange,
    onScopeChange,
    onSearch,
    onReset,
    onCreateClick,
}) {
    const handleSegmentChange = (value) => {
        if (value === statusAllValue) {
            onStatusChange(undefined);
        } else {
            onStatusChange(value);
        }
    };

    return (
        <div className="bg-gray-100 border border-gray-200 rounded-md px-4 py-3 mb-4">
            <div className="flex flex-col gap-3 w-full">
                <div className="flex flex-wrap gap-3 items-center">
                    <Input
                        placeholder="Tìm kiếm theo tên tài liệu"
                        value={query.keyword}
                        onChange={(e) => onKeywordChange(e.target.value)}
                        onPressEnter={onSearch}
                        style={{ width: 280 }}
                        allowClear
                    />
                    <Select
                        placeholder="Phân loại"
                        value={query.category}
                        onChange={onCategoryChange}
                        style={{ width: 220 }}
                        allowClear
                        options={categoryOptions}
                    />
                    <Select
                        placeholder="Phạm vi hiển thị"
                        value={query.visibilityScope ?? SCOPE_ALL_VALUE}
                        onChange={onScopeChange}
                        style={{ width: 220 }}
                        options={scopeFilterOptions}
                    />
                    <div className="flex-1" />
                    <Button icon={<FilterOutlined />} onClick={onSearch}>
                        Lọc
                    </Button>
                    <Tooltip title="Đặt lại tất cả bộ lọc">
                        <Button icon={<UndoOutlined />} onClick={onReset}>
                            Đặt lại
                        </Button>
                    </Tooltip>
                    {onCreateClick && (
                        <Button type="primary" onClick={onCreateClick}>
                            Tạo tài liệu
                        </Button>
                    )}
                </div>
                <div className="flex items-center gap-3 flex-wrap">
                    <Segmented
                        block
                        size="large"
                        style={{ width: "100%" }}
                        options={statusOptions}
                        value={statusValue}
                        onChange={handleSegmentChange}
                    />
                </div>
            </div>
        </div>
    );
}





