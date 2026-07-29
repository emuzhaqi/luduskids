import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { getOrder } from '../api/orders';

export default function OrderDetailPage() {
  const { id } = useParams();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getOrder(id)
      .then(setOrder)
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <p>Loading...</p>;
  if (!order) return <p>Order not found.</p>;

  return (
    <div className="order-detail">
      <h1>Order confirmed!</h1>
      <p>Order #{order.id} &middot; {new Date(order.createdAt).toLocaleString()}</p>
      <p>Status: {order.status}</p>
      <table>
        <thead>
          <tr>
            <th>Product</th>
            <th>Quantity</th>
            <th>Unit price</th>
          </tr>
        </thead>
        <tbody>
          {order.items.map((item) => (
            <tr key={item.productId}>
              <td>{item.productName}</td>
              <td>{item.quantity}</td>
              <td>${item.unitPrice.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <p className="cart-total">Total: ${order.totalAmount.toFixed(2)}</p>
    </div>
  );
}
