const flowbiteReact = require("flowbite-react/plugin/tailwindcss");

// tailwind.config.js
module.exports = {
  // content: ["./src/**/*.{js,jsx,ts,tsx}", ".flowbite-react\\class-list.json"],
  content: ["./src/**/*.{js,jsx,ts,tsx}", ".flowbite-react\\class-list.json"],

  theme: {
    extend: {
      // 🎨 Brand Colors
      colors: {
        brand: {
          DEFAULT: "#2563EB", // main brand (blue-600)
          light: "#3B82F6",   // lighter hover shade
          dark: "#1E40AF",    // darker for focus/active
        },
        accent: {
          DEFAULT: "#F59E0B", // accent (amber-500)
          light: "#FCD34D",
          dark: "#B45309",
        },
      },
      
       textColor: {
        'primary-color': '#5c6d9aff', // Custom color named 'primary-text'
        'secondary-color': '#6d3333ff', // Custom color named 'secondary-text'
        'tertiary-color': '#195b14ff', // Custom color named 'tertiary-text'
        'accent-color': '#FF5733', // Another custom color
      },

      // ✍️ Fonts
      fontFamily: {
        heading: ["times new roman", "arial", "sans-serif"],
        body: ["poppins",  "arial", "sans-serif"],
      },

      // 📏 Spacing (add bigger gaps for hero sections etc.)
      spacing: {
        18: "4.5rem",
        128: "32rem",
      },

      // ⭕ Border Radius
      borderRadius: {
        xl: "1rem",
        "2xl": "1.5rem",
      },

      // 🌑 Shadows (nice for event cards, modals)
      boxShadow: {
        card: "0 4px 12px rgba(92, 119, 77, 0.08)",
        elevated: "0 6px 20px rgba(0,0,0,0.12)",
      },

      // 📱 Breakpoints (optional extra small + very large screen)
      screens: {
        xs: "480px",
        "3xl": "1600px",
      },
    },
  },

  // plugins: [flowbiteReact],
    plugins: [require('flowbite/plugin'), flowbiteReact],
}