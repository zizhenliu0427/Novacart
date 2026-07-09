export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  productSlug?: string;
  price: number;
  quantity: number;
  lineTotal: number;
}

export interface Order {
  id: string;
  orderNumber: string;
  subtotal: number;
  shippingCost: number;
  tax: number;
  total: number;
  currency: string;
  currentStatus: string;
  createdAt: string;
  items?: OrderItem[];
}
