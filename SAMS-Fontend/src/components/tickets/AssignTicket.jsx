import React, { useState } from "react";
import { Form, Select, Button, Card, message, Typography } from "antd";
import api from "../../lib/apiClient";
import ticketsApi from "../../features/tickets/ticketsApi";

const { Text } = Typography;

export default function AssignTicket({ ticketId, onAssigned, usernamePlaceholder, currentAssigned, disabled }) {
    const [form] = Form.useForm();
    const [loading, setLoading] = useState(false);
    const [options, setOptions] = useState([]);
    const [searching, setSearching] = useState(false);
    const [selectedUser, setSelectedUser] = useState(null);

    const searchUsers = async (q) => {
        const query = String(q || "").trim();
        if (!query) {
            setOptions([]);
            return;
        }
        setSearching(true);
        try {
            const res = await api.get('/Users/lookup', { params: { username: query, page: 1, pageSize: 10 } });
            const items = res?.data?.items || [];
            setOptions(items.map(u => ({ label: u.username, value: u.userId, data: u })));
        } catch (e) {
            // Lookup users failed
        } finally {
            setSearching(false);
        }
    };

    const submit = async (values) => {
        const userId = values.assignee;
        if (!userId) {
            message.warning("Chọn username cần gán");
            return;
        }
        const confirmed = window.confirm('Bạn có chắc muốn gán ticket này cho người dùng đã chọn?');
        if (!confirmed) return;
        setLoading(true);
        try {
            await ticketsApi.assign({ ticketId, assignedToUserId: userId });
            message.success("Đã gán ticket thành công");
            form.resetFields();
            onAssigned?.();
        } catch (e) {
            message.error(e?.response?.data?.message || "Gán ticket thất bại");
        } finally {
            setLoading(false);
        }
    };

    return (
        <Form form={form} layout="vertical" onFinish={submit}>
            {currentAssigned && (
                <Card size="small" className="mb-3">
                    <div className="space-y-1 text-sm">
                        <div><Text strong>Assign:</Text> <Text>{currentAssigned.username}</Text></div>
                        <div><Text strong>Email:</Text> <Text>{currentAssigned.email || '-'}</Text></div>
                        <div><Text strong>Phone:</Text> <Text>{currentAssigned.phone || '-'}</Text></div>
                        <div><Text strong>DOB:</Text> <Text>{currentAssigned.dob || '-'}</Text></div>
                    </div>
                </Card>
            )}
            <Form.Item name="assignee" label="Username" rules={[{ required: true, message: "Chọn username" }]}>
                <Select
                    showSearch
                    placeholder={usernamePlaceholder}
                    filterOption={false}
                    onSearch={searchUsers}
                    options={options}
                    loading={searching}
                    onChange={(val) => {
                        const found = options.find(o => o.value === val);
                        setSelectedUser(found?.data || null);
                    }}
                    allowClear
                    showArrow={false}
                    disabled={disabled}
                />
            </Form.Item>
            {selectedUser && (
                <Card size="small" className="mb-3">
                    <div className="space-y-1 text-sm">
                        <div><Text strong>Email:</Text> <Text>{selectedUser.email || '-'}</Text></div>
                        <div><Text strong>Phone:</Text> <Text>{selectedUser.phone || '-'}</Text></div>
                        <div><Text strong>DOB:</Text> <Text>{selectedUser.dob || '-'}</Text></div>
                    </div>
                </Card>
            )}
            <Form.Item className="!mb-0">
                <Button type="primary" htmlType="submit" loading={loading} block
                    disabled={disabled}
                >
                    Gán Ticket
                </Button>
            </Form.Item>
        </Form>
    );
}


