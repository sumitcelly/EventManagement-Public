export default function SimulationInfo() {
  return (
    <div className="p-4 border rounded-lg shadow-md bg-brand-neutral text-center mb-4">
      <div className="text-xs font-bold mb-2 uppercase tracking-wide">Checkout simulation mode</div>
      <div className="font-body text-sm">
        You are currently in checkout simulation mode. Please use the Stripe test card{' '}
        <span className="font-semibold">4242 4242 4242 4242</span> to simulate a transaction.
      </div>
    </div>
  );
}
