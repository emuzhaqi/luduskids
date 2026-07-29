import { useEffect, useState } from 'react';
import { getOrders, updateOrderStatus } from '../../api/orders';

const STATUSES = ['Pending', 'Shipped', 'Delivered', 'Cancelled'];

export default function AdminOrdersPage() {
  const [orders, setOrders] = useState([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    setOrders(await getOrders());
  };

  useEffect(() => {
    load().finally(() => setLoading(false));
  }, []);

  const handleStatusChange = async (id, status) => {
    await updateOrderStatus(id, status);
    await load();
  };

  if (loading) return <p>Loading...</p>;

  return (
    <div className="admin-page">
      <h1>Manage orders</h1>
      <table>
        <thead>
          <tr>
            <th>Order</th>
            <th>Customer</th>
            <th>Date</th>
            <th>Total</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {orders.map((order) => (
            <tr key={order.id}>
              <td>#{order.id}</td>
              <td>{order.userEmail}</td>
              <td>{new Date(order.createdAt).toLocaleDateString()}</td>
              <td>${order.totalAmount.toFixed(2)}</td>
              <td>
                <select value={order.status} onChange={(e) => handleStatusChange(order.id, e.target.value)}>
                  {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
                </select>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
