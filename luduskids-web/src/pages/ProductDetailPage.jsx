import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { getProduct } from '../api/products';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';

export default function ProductDetailPage() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [adding, setAdding] = useState(false);
  const { user } = useAuth();
  const { addItem } = useCart();
  const navigate = useNavigate();

  useEffect(() => {
    getProduct(id)
      .then(setProduct)
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) return <p>Loading...</p>;
  if (!product) return <p>Toy not found.</p>;

  const handleAddToCart = async () => {
    if (!user) {
      navigate('/login');
      return;
    }
    setAdding(true);
    try {
      await addItem(product.id, 1);
    } finally {
      setAdding(false);
    }
  };

  return (
    <div className="product-detail">
      <img src={product.imageUrl} alt={product.name} />
      <div>
        <h1>{product.name}</h1>
        <p className="product-card-category">{product.categoryName}</p>
        <p>{product.description}</p>
        <p className="product-card-price">${product.price.toFixed(2)}</p>
        <p>{product.stockQuantity > 0 ? `${product.stockQuantity} in stock` : 'Out of stock'}</p>
        <button onClick={handleAddToCart} disabled={adding || product.stockQuantity === 0}>
          {adding ? 'Adding...' : 'Add to cart'}
        </button>
      </div>
    </div>
  );
}
