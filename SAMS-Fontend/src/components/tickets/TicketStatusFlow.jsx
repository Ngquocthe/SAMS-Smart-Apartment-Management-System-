import React from "react";
import { Card, Steps, Button, Popconfirm } from "antd";
import { ClockCircleOutlined } from "@ant-design/icons";

export const STATUS_FLOW = ["Mới tạo", "Đã tiếp nhận", "Đang xử lý", "Hoàn thành", "Đã đóng"];

export default function TicketStatusFlow({
    ticket,
    onStatusChange,
    statusUpdating,
    disabled = false
}) {
    const getStatusIndex = (s) => {
        const idx = STATUS_FLOW.indexOf(String(s || ''));
        return idx >= 0 ? idx : 0;
    };
    const currentIndex = getStatusIndex(ticket?.status);
    const isLastStep = currentIndex >= STATUS_FLOW.length - 1;
    const nextIndex = Math.min(currentIndex + 1, STATUS_FLOW.length - 1);
    const canAdvance = !isLastStep && !disabled;

    return (
        <Card
            title={
                <div className="flex items-center space-x-2">
                    <ClockCircleOutlined className="text-blue-500" />
                    <span>Quy trình xử lý</span>
                </div>
            }
            extra={
                <Popconfirm
                    title={`Xác nhận chuyển trạng thái sang "${STATUS_FLOW[nextIndex]}"?`}
                    onConfirm={() => onStatusChange(nextIndex)}
                    okButtonProps={{ loading: statusUpdating }}
                    disabled={!canAdvance}
                >
                    <Button
                        type="primary"
                        disabled={!canAdvance}
                        loading={statusUpdating}
                    >
                        Cập nhật
                    </Button>
                </Popconfirm>
            }
            className="shadow-sm"
        >
            <Steps
                size="small"
                current={currentIndex}
                items={STATUS_FLOW.map((s, idx) => ({
                    title: s,
                    status: idx < currentIndex ? "finish" : idx === currentIndex ? "process" : "wait",
                }))}
            />
        </Card>
    );
}


