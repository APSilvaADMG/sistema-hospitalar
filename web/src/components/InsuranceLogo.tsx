import { useMemo, useState } from 'react';

type Props = {
  name: string;
  logoUrl?: string | null;
  size?: number;
  className?: string;
};

function getInitials(name: string): string {
  const words = name
    .replace(/[^\p{L}\s]/gu, ' ')
    .split(/\s+/)
    .filter(Boolean);

  if (words.length === 0) return '?';
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  return `${words[0][0]}${words[1][0]}`.toUpperCase();
}

function stringToColor(value: string): string {
  let hash = 0;
  for (let i = 0; i < value.length; i += 1) {
    hash = value.charCodeAt(i) + ((hash << 5) - hash);
  }

  const hue = Math.abs(hash) % 360;
  return `hsl(${hue} 55% 42%)`;
}

export function InsuranceLogo({ name, logoUrl, size = 48, className = '' }: Props) {
  const [failed, setFailed] = useState(false);
  const initials = useMemo(() => getInitials(name), [name]);
  const color = useMemo(() => stringToColor(name), [name]);

  if (!logoUrl || failed) {
    return (
      <span
        className={`insurance-logo insurance-logo-fallback ${className}`.trim()}
        style={{ width: size, height: size, background: color, fontSize: size * 0.34 }}
        aria-hidden
      >
        {initials}
      </span>
    );
  }

  return (
    <img
      className={`insurance-logo ${className}`.trim()}
      src={logoUrl}
      alt=""
      width={size}
      height={size}
      loading="lazy"
      onError={() => setFailed(true)}
    />
  );
}
