import React, { useEffect, useState } from 'react';
import service from './service.js';
import './App.css';

function App() {
  const [newTodo, setNewTodo] = useState("");
  const [todos, setTodos] = useState([]);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  
  // מצבים לניהול התצוגה
  const [isLoggedIn, setIsLoggedIn] = useState(!!localStorage.getItem("token"));
  const [isRegisterMode, setIsRegisterMode] = useState(false);

  async function getTodos() {
    try {
      const todos = await service.getTasks();
      setTodos(todos);
    } catch (error) {
      console.error("Failed to fetch todos", error);
    }
  }

  // פונקציית התחברות
  async function loginUser(e) {
    e.preventDefault();
    try {
      await service.login(username, password);
      setIsLoggedIn(true);
      setUsername("");
      setPassword("");
    } catch (error) {
      alert("שגיאה בהתחברות: " + (error.response?.data?.message || "בדקי שם משתמש וסיסמה"));
    }
  }

  async function registerUser(e) {
    e.preventDefault();
    try {
      await service.register(username, password);
      alert("נרשמת בהצלחה! כעת התחברי.");
      setIsRegisterMode(false); // מעביר אותך אוטומטית למסך התחברות
    } catch (error) {
      alert("שגיאה ברישום: " + error.message);
    }
  }

  // --- שאר הפונקציות (create, update, delete) נשארות אותו דבר ---
  async function createTodo(e) {
    e.preventDefault();
    await service.addTask(newTodo);
    setNewTodo("");
    await getTodos();
  }

  async function updateCompleted(todo, isComplete) {
    await service.setCompleted(todo.id, isComplete);
    await getTodos();
  }

  async function deleteTodo(id) {
    await service.deleteTask(id);
    await getTodos();
  }

  useEffect(() => {
    if (isLoggedIn) {
      getTodos();
    }
  }, [isLoggedIn]); // ירוץ כל פעם שמתחברים

  // פונקציית יציאה
  const logout = () => {
    localStorage.removeItem("token");
    setIsLoggedIn(false);
    setTodos([]);
  };

  // --- תצוגה של מסך התחברות/הרשמה ---
  if (!isLoggedIn) {
    return (
      <section className="registration-container">
        <h2>{isRegisterMode ? "הרשמה למערכת" : "כניסה למערכת"}</h2>
        <form onSubmit={isRegisterMode ? registerUser : loginUser}>
          <input 
            className="registration-input"
            placeholder="שם משתמש" 
            value={username} 
            onChange={(e) => setUsername(e.target.value)} 
          />
          <input
            className="registration-input" 
            type="password" 
            placeholder="סיסמה" 
            value={password} 
            onChange={(e) => setPassword(e.target.value)} 
          />
          <button type="submit" className="registration-button">
            {isRegisterMode ? "צור חשבון" : "התחבר"}
          </button>
        </form>
        <p onClick={() => setIsRegisterMode(!isRegisterMode)} style={{cursor: 'pointer', color: 'blue', marginTop: '10px'}}>
          {isRegisterMode ? "כבר יש לך חשבון? התחבר כאן" : "משתמש חדש? הירשם כאן"}
        </p>
      </section>
    );
  }

  // --- תצוגה של רשימת המטלות (רק אם מחוברים) ---
  return (
    <section className="todoapp">
      <button onClick={logout} style={{float: 'right', margin: '10px'}}>יציאה</button>
      <header className="header">
        <h1>todos</h1>
        <form onSubmit={createTodo}>
          <input className="new-todo" placeholder="Well, let's take on the day" value={newTodo} onChange={(e) => setNewTodo(e.target.value)} />
        </form>
      </header>
      <section className="main" style={{ display: "block" }}>
        <ul className="todo-list">
          {todos.map(todo => (
            <li className={todo.isComplete ? "completed" : ""} key={todo.id}>
              <div className="view">
                <input className="toggle" type="checkbox" checked={todo.isComplete} onChange={(e) => updateCompleted(todo, e.target.checked)} />
                <label>{todo.name}</label>
                <button className="destroy" onClick={() => deleteTodo(todo.id)}></button>
              </div>
            </li>
          ))}
        </ul>
      </section>
    </section>
  );
}

export default App;