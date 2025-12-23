import React from 'react';
import { Container, Row, Col, Card } from 'react-bootstrap';

export default function Buildings() {
  return (
    <Container fluid className="p-4">
      <Row className="mb-4">
        <Col>
          <h2 className="mb-2">Quản lý tòa nhà</h2>
          <p className="text-muted">Quản lý thông tin các tòa nhà trong hệ thống</p>
        </Col>
      </Row>
      
      <Row>
        <Col>
          <Card>
            <Card.Body>
              <h5>Danh sách tòa nhà</h5>
              <p className="text-muted">Chức năng quản lý tòa nhà đang được phát triển...</p>
            </Card.Body>
          </Card>
        </Col>
      </Row>
    </Container>
  );
}
