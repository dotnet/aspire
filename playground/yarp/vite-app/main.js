import './style.css';

let count = 0;
const button = document.getElementById('counter');

button.addEventListener('click', () => {
  count++;
  button.textContent = `Count is: ${count}`;
});
