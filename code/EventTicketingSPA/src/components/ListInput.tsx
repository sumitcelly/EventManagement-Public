import { useState } from "react";
import { Button, TextInput, Badge } from "flowbite-react";

export default function ListInput({items,onChange}: {items:string[],onChange:(items:string[])=>void}) 
{
  const [input, setInput] = useState("");

  const addItem = () => {
    if (!input.trim()) return;    
    onChange([...items, input.trim()]);
    setInput("");
  };

  const removeItem = (index: number) => {
    onChange(items.filter((_, i) => i !== index));
  };

  return (
    <div className="space-y-3">
      <div className="flex gap-2">
        <TextInput
          placeholder="Add an item"
          value={input}
          onChange={(e) => setInput(e.target.value)}
        />
        <Button onClick={addItem}>Add</Button>
      </div>

      <div className="flex flex-wrap gap-2">
        {items && items.map((item, i) => (
          <Badge
            key={i}
            color="info"
            icon={() => (
              <button onClick={() => removeItem(i)} className="ml-1 text-xs">
                ✕
              </button>
            )}
          >
            {item}
          </Badge>
        ))}
      </div>
    </div>
  );
}
