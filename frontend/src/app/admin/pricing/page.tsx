import { ComingSoon } from '@/components/ui/ComingSoon';

export default function AdminPricingPage() {
  return (
    <ComingSoon title="Pricing rules" item="P2-5">
      Configure dynamic pricing (percent / flat / fixed) per product or category, with time windows.
      Feeds <code>IPricingService</code>; calls <code>/api/admin/price-rules</code>.
    </ComingSoon>
  );
}
