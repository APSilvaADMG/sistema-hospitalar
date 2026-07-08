type PersonAvatarProps = {
  name: string;
  photoData?: string | null;
  size?: number;
  className?: string;
};

function initials(name: string) {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? '')
    .join('');
}

export function PersonAvatar({ name, photoData, size = 40, className = '' }: PersonAvatarProps) {
  const style = { width: size, height: size, fontSize: Math.max(10, size * 0.32) };

  if (photoData) {
    return (
      <img
        src={photoData}
        alt={name}
        className={`person-avatar person-avatar-photo ${className}`}
        style={style}
      />
    );
  }

  return (
    <span className={`person-avatar ${className}`} style={style} aria-hidden>
      {initials(name)}
    </span>
  );
}
