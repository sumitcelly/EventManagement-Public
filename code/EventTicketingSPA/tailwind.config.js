const flowbiteReact = require("flowbite-react/plugin/tailwindcss");

// tailwind.config.js
module.exports = {
  
  content: ["./src/**/*.{js,jsx,ts,tsx}",
            "node_modules/flowbite-typography/**/*.{js,jsx,ts,tsx}",
           ".flowbite-react/class-list.json"],

  theme: {
    extend: {
      // 🎨 Brand Colors
      colors: {
        brand: {
          DEFAULT: "#2563EB", // button color
          light: "#b6c690ff",   // main color for site
          dark: "#1E40AF",    // darker for focus/active
          neutral: "#b6c690ff", // background for events page. same as "light"
          neutrallight:  "#20c4aeff", // darker gray for text
          panelbg:"#eaead7",
          navbg:"rgb(14, 196, 228)"
        },
        accent: {
          DEFAULT: "#F59E0B", // accent (amber-500)
          light: "#FCD34D",
          dark: "#B45309",
        },
      },
      
       textColor: {
        'link-color':'hsla(224, 90%, 27%, 0.83)',
        'primary-color': '#5c6d9aff', // Custom color named 'primary-text'
        'secondary-color': '#6d3333ff', // Custom color named 'secondary-text'
        'tertiary-color': '#195b14ff', // Custom color named 'tertiary-text'
        'accent-color': '#FF5733', // Another custom color
        'go-color':'#34a12aff',
        'error-color':'rgb(179, 25, 25)'
      },

      // ✍️ Fonts
      fontFamily: {
        heading: ['Montserrat', 'sans-serif'],
        body: ['Poppins', 'sans-serif'],
        accent: ['Pacifico', 'cursive'],
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
    plugins: [require('flowbite/plugin'), flowbiteReact, require("flowbite-typography"),],
    
}