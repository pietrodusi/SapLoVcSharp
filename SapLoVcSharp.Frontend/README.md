# SAP LO-VC Configuration Manager - Frontend

Modern web application for managing SAP LO-VC (Logistics - Variant Configuration) materials, classes, characteristics, and configurations.

## Technology Stack

- **Vite** - Fast build tool and dev server
- **React 18** - UI framework
- **TypeScript** - Type-safe JavaScript
- **Material-UI (MUI)** - React component library
- **Tailwind CSS** - Utility-first CSS framework
- **React Router** - Client-side routing
- **Axios** - HTTP client
- **Monaco Editor** - Code editor for SAP dependency language

## Prerequisites

- Node.js 18+ and npm
- Backend API running at `https://localhost:5001`

## Getting Started

### 1. Install Dependencies

```bash
npm install
```

### 2. Start Development Server

```bash
npm run dev
```

The application will be available at **http://localhost:3000**

### 3. Build for Production

```bash
npm run build
```

The build output will be in the `dist` folder.

### 4. Preview Production Build

```bash
npm run preview
```

## Application Features

### 1. Material Selection Page

- View all materials
- Create new materials
- Select a material to manage

### 2. Classification Data Tab

- View all characteristics assigned to the material
- Add new characteristics to classes
- Configure characteristic properties:
  - Name, description, data type
  - Required/Restrictable flags
  - Allowed values

### 3. Object Dependencies Tab

- Create constraints and procedures
- Full SAP dependency language syntax support
- Monaco code editor with syntax highlighting
- Sample code templates for constraints and procedures

### 4. Configuration Demo Tab

- Interactive configuration execution
- Auto-execute configuration when values change
- Dropdowns for each characteristic showing only allowed values
- Real-time configuration results:
  - Success/failure status
  - Complete/incomplete status
  - Stable/unstable status
  - Final characteristic values
  - Execution duration and cycle count
  - Error messages

## API Configuration

The application connects to the backend API at `https://localhost:5001`.

- In **development**: Uses Vite proxy (configured in `vite.config.ts`)
- In **production**: Direct connection to `https://localhost:5001/api`

To change the API URL, edit `src/api/client.ts`.

## Project Structure

```
src/
├── api/
│   └── client.ts              # API client with all endpoints
├── components/
│   ├── MaterialSelection.tsx  # Main material list page
│   ├── MaterialDetail/
│   │   ├── MaterialDetail.tsx      # Material detail container
│   │   ├── ClassificationTab.tsx   # Characteristics management
│   │   ├── DependenciesTab.tsx     # Constraints/procedures
│   │   └── ConfigurationTab.tsx    # Configuration demo
│   └── shared/
│       └── Layout.tsx         # App layout with header
├── types/
│   └── api.ts                 # TypeScript type definitions
├── App.tsx                    # Main app with routing
├── main.tsx                   # App entry point
└── index.css                  # Global styles + Tailwind
```

## Available Scripts

- `npm run dev` - Start development server
- `npm run build` - Build for production
- `npm run preview` - Preview production build
- `npm run lint` - Run ESLint

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)

## Troubleshooting

### API Connection Issues

If you see "Failed to load materials" errors:

1. Ensure the backend API is running at `https://localhost:5001`
2. Check browser console for CORS errors
3. Verify the API is accessible: `curl https://localhost:5001/health`

### Build Errors

If you encounter TypeScript errors:

```bash
# Clear node_modules and reinstall
rm -rf node_modules package-lock.json
npm install
```

## Development Tips

- The app uses hot module replacement (HMR) - changes are reflected instantly
- React DevTools browser extension is recommended for debugging
- Monaco Editor loads lazily for better performance
- All API calls are typed with TypeScript interfaces

## Future Enhancements

- [ ] Delete material functionality
- [ ] Dependency net management
- [ ] Configuration profile creation
- [ ] Variant table management UI
- [ ] Export/import configurations
- [ ] User authentication
- [ ] Dark mode theme
