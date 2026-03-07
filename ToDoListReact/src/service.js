import axios from 'axios';


axios.defaults.baseURL = process.env.REACT_APP_API_URL
const savedToken = localStorage.getItem("token");

// הוספת הטוקן לכל בקשה שיוצאת
axios.interceptors.request.use(config => {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

axios.interceptors.response.use(
  res => res,
  error => {
    // בדיקה האם השגיאה היא 401 (Unauthorized)
    if (error.response && error.response.status === 401) {
      console.error("לא מורשה! מעביר לדף התחברות...");
      
      // מחיקת הטוקן הישן (כי הוא כנראה לא תקף)
      localStorage.removeItem("token");
      
      // העברה לדף הלוגין
      window.location.href = "/login"; 
    }
    
    console.error('API Error', error.message);
    return Promise.reject(error);
  }
);

export default {
  getTasks: async () => {
    const result = await axios.get(`/items`)    
    return result.data;
  },

  addTask: async(name)=>{
   // console.log('addTask', name)   
        const result = await axios.post(`/items`, { name: name, isComplete: false });
        return result.data;  // מחזיר את התוצאה מהשרת, אפשר להחזיר את המשימה החדשה
   
},

  setCompleted: async(id, isComplete)=>{
   // console.log('setCompleted', {id, isComplete,name})   
        const result = await axios.put(`/items/${id}`, { isComplete: isComplete });
        return result.data;  // מחזיר את המשימה המעודכנת
     
},

  deleteTask:async(id)=>{    
        const result = await axios.delete(`/items/${id}`);
        return result.data;  // מחזיר את התוצאה מהשרת (למשל, הודעה שהמשימה נמחקה)   
},

//רישום
register: async (username, password) => {
    const result = await axios.post(`/register`, { username, password });
    return result.data;
  },
//כניסה
login: async (username, password) => {
  const response = await axios.post(`/login`, { username, password });
  const token = response.data.token;
  
  // שומרים את הטוקן בזיכרון של הדפדפן
  localStorage.setItem("token", token);
  
  // מגדירים ל-axios לשלוח את הטוקן הזה מעכשיו בכל בקשה
  axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  
  return response.data;
}  

};
