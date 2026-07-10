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

/** Admin order list row — compact, no line items. */
export interface AdminOrderSummary {
  id: string;
  orderNumber: string;
  userId: string;
  customerEmail: string;
  total: number;
  currency: string;
  currentStatus: string;
  itemCount: number;
  createdAt: string;
}

/** Admin order detail — includes customer name + line items. */
export interface AdminOrderDetail extends Omit<AdminOrderSummary, 'itemCount'> {
  customerName: string;
  subtotal: number;
  shippingCost: number;
  tax: number;
  updatedAt: string | null;
  items: OrderItem[];
}

export interface UpdateOrderStatusBody {
  toStatus: string;
  notes?: string;
}
