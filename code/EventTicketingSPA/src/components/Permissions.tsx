import React, { useEffect, useState } from "react";

interface PermissionsProps {
  permissionsList?: string;
  onChange: (perms: string) => void;
}

const allPermissions = ["Admin", "RestrictedAdmin", "Checkin"];

export default function Permissions({ permissionsList, onChange }: PermissionsProps) {
  // Convert the comma-separated string into a Set for easy lookup
  const [selected, setSelected] = useState<Set<string>>(new Set());

  // Initialize or update state when permissionsList changes
  useEffect(() => {
    if (permissionsList) {
      const perms = permissionsList.split(",").map((p) => p.trim());
      setSelected(new Set(perms));
    }
    else{
       setSelected(new Set([]));
    }
  }, [permissionsList]);

  // Whenever selected changes, send comma-separated list back up
  useEffect(() => {
    onChange(Array.from(selected).join(","));
  }, [selected, onChange]);

  const handleToggle = (perm: string) => {
    setSelected((prev) => {
      const updated = new Set(prev);
      if (updated.has(perm)) {
        updated.delete(perm);
      } else {
        updated.add(perm);
      }
      return updated;
    });
  };

  return (
    <div className="flex flex-col space-y-2">
      {allPermissions.map((perm) => (
        <label key={perm} className="flex items-center space-x-2">
          <input
            type="checkbox"
            checked={selected.has(perm)}
            onChange={() => handleToggle(perm)}
          />
          <span>{perm}</span>
        </label>
      ))}
    </div>
  );
}
