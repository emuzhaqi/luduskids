import { Link } from 'react-router-dom';

export default function ProductCard({ product }) {
  return (
    <Link to={`/products/${product.id}`} className="product-card">
      <img src={product.imageUrl} alt={product.name} />
      <h3>{product.name}</h3>
      <p className="product-card-category">{product.categoryName}</p>
      <p className="product-card-price">${product.price.toFixed(2)}</p>
    </Link>
  );
}
