import React, { useState, useEffect } from "react";

export interface PasswordStatus {
  password: string;
  confirm: string;
  valid: boolean;
  errors: string[];
}

interface PasswordFieldsProps {
  onChange?: (state: PasswordStatus) => void;
}

const PasswordFields: React.FC<PasswordFieldsProps> = ({ onChange }) => {
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [errors, setErrors] = useState<string[]>([]);

  useEffect(() => {
    const newErrors: string[] = [];

    if (password.length < 6)
      newErrors.push("Password must be at least 6 characters.");
    if (!/[A-Z]/.test(password))
      newErrors.push("Password must contain an uppercase letter.");
    if (!/[a-z]/.test(password))
      newErrors.push("Password must contain a lowercase letter.");
    if (!/[0-9]/.test(password))
      newErrors.push("Password must contain a number.");
    if (!/[^A-Za-z0-9]/.test(password))
      newErrors.push("Password must contain a special character.");
    if (password !== confirm)
      newErrors.push("Passwords do not match.");
    //console.log('password and confirm',password,confirm);
    setErrors(newErrors);

    onChange?.({
      password,
      confirm,
      valid: newErrors.length === 0,
      errors: newErrors
    });
  }, [password, confirm]);

  return (
    <div>
        <div>
          <label className="block text-sm font-medium">Password</label>
          <input
            type="password"
            value={password}
            autoComplete="new-password"
            onChange={(e) => setPassword(e.target.value)}
            className="mt-1 block w-full border rounded px-3 py-2"
          />
        </div>
        <div>
          <label className="block text-sm font-medium mt-4">Confirm Password</label>
          <input
            type="password"
            value={confirm}
            autoComplete="new-password"
            onChange={(e) => setConfirm(e.target.value)}
            className="mt-1 block w-full border rounded px-3 py-2"
          />
        </div>

        {errors.length > 0 && (
            <ul style={{ color: "red", marginTop: 8 }}>
            {errors.map((err, i) => (
                <li key={i}>{err}</li>
            ))}
            </ul>
        )}
    </div>
  );
};

export default PasswordFields;
