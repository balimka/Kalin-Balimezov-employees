import React, { useState } from 'react';
import './App.css';

interface ProjectCollaboration {
  projectId: number;
  employee1Id: number;
  employee2Id: number;
  daysWorkedTogether: number;
  overlapStart: string;
  overlapEnd: string;
}

interface EmployeePairResult {
  employee1Id: number;
  employee2Id: number;
  totalDaysWorkedTogether: number;
  commonProjects: ProjectCollaboration[];
}

function App() {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<EmployeePairResult | string | null>(null);
  const [error, setError] = useState<string>('');
  const [simpleFormat, setSimpleFormat] = useState(false);

  const API_BASE_URL = 'https://localhost:7207';

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file && file.name.toLowerCase().endsWith('.csv')) {
      setSelectedFile(file);
      setError('');
      setResult(null);
    }
  };

  const analyzeFile = async () => {
    if (!selectedFile) return;

    setLoading(true);
    setError('');
    setResult(null);

    try {
      const formData = new FormData();
      formData.append('file', selectedFile);
      
      const endpoint = simpleFormat ? '/api/analyze-employees/simple' : '/api/analyze-employees';
      const response = await fetch(`${API_BASE_URL}${endpoint}`, {
        method: 'POST',
        body: formData,
      });

      if (!response.ok) {
        throw new Error(await response.text());
      }

      const data = simpleFormat ? await response.text() : await response.json();
      setResult(data);

    } catch (err) {
      setError(err instanceof Error ? err.message.replace(/^"|"$/g, '') : 'Analysis failed');
    } finally {
      setLoading(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString();
  };

  return (
    <div className="App">
      <header className="App-header">
        <h1>Employee Collaboration Analyzer</h1>
        <p>Find employee pairs who worked together the longest</p>
      </header>

      <main className="App-main">
        <div className="upload-section">
          <input
            type="file"
            accept=".csv"
            onChange={handleFileChange}
            className="file-input"
          />
          
          <div className="format-selection">
            <label>
              <input
                type="checkbox"
                checked={simpleFormat}
                onChange={(e) => setSimpleFormat(e.target.checked)}
              />
              Simple format
            </label>
          </div>
          
          <button 
            onClick={analyzeFile}
            disabled={!selectedFile || loading}
            className="analyze-button"
          >
            {loading ? 'Analyzing...' : 'Analyze'}
          </button>
        </div>

        {error && (
          <div className="error-message">
            <strong>Error:</strong> {error}
          </div>
        )}

        {result && typeof result === 'string' && (
          <div className="simple-result-section">
            <h2>Result</h2>
            <div className="simple-result">
              <strong>{result}</strong>
            </div>
          </div>
        )}

        {result && typeof result === 'object' && (
          <div className="results-section">
            <h2>Result</h2>
            
            <div className="pair-summary">
              <p><strong>Employees:</strong> {result.employee1Id}, {result.employee2Id}</p>
              <p><strong>Days worked together:</strong> {result.totalDaysWorkedTogether}</p>
              <p><strong>Projects:</strong> {result.commonProjects.length}</p>
            </div>

            <div className="project-details">
              <h3>Project Details</h3>
              <table className="projects-table">
                <thead>
                  <tr>
                    <th>Project</th>
                    <th>Days</th>
                    <th>Period</th>
                  </tr>
                </thead>
                <tbody>
                  {result.commonProjects.map((project, index) => (
                    <tr key={index}>
                      <td>{project.projectId}</td>
                      <td>{project.daysWorkedTogether}</td>
                      <td>{formatDate(project.overlapStart)} - {formatDate(project.overlapEnd)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        <div className="help-section">
          <h3>CSV Format</h3>
          <p>Format: <code>EmpID, ProjectID, DateFrom, DateTo</code></p>
          <p>DateTo can be NULL. Various date formats supported.</p>
          
          <h4>Sample:</h4>
          <pre>
{`143, 12, 2013-11-01, 2014-01-05
218, 10, 2012-05-16, NULL
143, 10, 2009-01-01, 2011-04-27`}
          </pre>
        </div>
      </main>
    </div>
  );
}

export default App;