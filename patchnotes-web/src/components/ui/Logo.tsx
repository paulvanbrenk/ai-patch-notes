export function Logo() {
  return (
    <svg
      viewBox="0 0 24 24"
      className="w-8 h-8"
      fill="none"
      strokeWidth="1.5"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {/* Notebook shell — muted, structural */}
      <g className="text-text-secondary" stroke="currentColor">
        <path d="M2 6h4" />
        <path d="M2 10h4" />
        <path d="M2 14h4" />
        <path d="M2 18h4" />
        <rect width="18" height="22" x="3" y="1" rx="2" />
      </g>
      {/* Package box — brand accent, focal point */}
      <g className="text-brand-500 dark:text-brand-400" stroke="currentColor">
        <path d="M19 8.5a1 1 0 0 0-.5-.87l-4.5-2.5a1 1 0 0 0-1 0l-4.5 2.5A1 1 0 0 0 8 8.5v5a1 1 0 0 0 .5.87l4.5 2.5a1 1 0 0 0 1 0l4.5-2.5a1 1 0 0 0 .5-.87Z" />
        <path d="m8.3 7.5 5.7 3.2 5.7-3.2" />
        <path d="M14 17V10.7" />
      </g>
    </svg>
  )
}
