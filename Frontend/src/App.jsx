import React, { useState, useEffect, useCallback } from 'react';
import axios from 'axios';
import { toast, ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.css';

const apiClient = axios.create({
    baseURL: 'http://localhost:8000/gateway',
});

const statusConfig = {
    New: { text: 'Pending', className: 'bg-yellow-500 text-white' },
    Finished: { text: 'Completed', className: 'bg-green-500 text-white' },
    Cancelled: { text: 'Cancelled', className: 'bg-red-500 text-white' },
};

function App() {
    const [userId, setUserId] = useState(null);
    const [balance, setBalance] = useState(0);
    const [orders, setOrders] = useState([]);
    const [loading, setLoading] = useState({ balance: true, orders: true });
    const [depositModalOpen, setDepositModalOpen] = useState(false);
    const [orderModalOpen, setOrderModalOpen] = useState(false);
    const [depositAmount, setDepositAmount] = useState('');
    const [newOrder, setNewOrder] = useState({ description: '', amount: '' });

    const showToast = (message, type = 'success') => {
        toast[type](message, {
            position: 'bottom-center',
            autoClose: 3000,
            hideProgressBar: true,
            closeOnClick: true,
            pauseOnHover: true,
            theme: 'colored',
        });
    };

    const getBalance = useCallback(async (currentUserId) => {
        if (!currentUserId) return;
        setLoading((prev) => ({ ...prev, balance: true }));
        try {
            const response = await apiClient.get(
                `/payments/accounts/balance/${currentUserId}`
            );
            setBalance(response.data.balance);
        } catch (err) {
            if (err.response && err.response.status === 404) {
                try {
                    await apiClient.post('/payments/accounts', {
                        userId: currentUserId,
                    });
                    setBalance(0);
                } catch (creationErr) {
                    if (
                        creationErr.response &&
                        creationErr.response.status !== 409
                    ) {
                        showToast(
                            `Failed to create account: ${creationErr.message}`,
                            'error'
                        );
                    }
                }
            } else {
                showToast(
                    `Network error fetching balance: ${err.message}`,
                    'error'
                );
            }
        } finally {
            setLoading((prev) => ({ ...prev, balance: false }));
        }
    }, []);

    const getOrders = useCallback(async (currentUserId) => {
        if (!currentUserId) return;
        setLoading((prev) => ({ ...prev, orders: true }));
        try {
            const response = await apiClient.get(
                `/orders/user/${currentUserId}`
            );
            setOrders(response.data);
        } catch (err) {
            if (err.response && err.response.status === 404) {
                setOrders([]);
            } else {
                showToast(
                    `Network error fetching orders: ${err.message}`,
                    'error'
                );
            }
        } finally {
            setLoading((prev) => ({ ...prev, orders: false }));
        }
    }, []);

    const handleDeposit = async () => {
        if (!depositAmount || parseFloat(depositAmount) <= 0) {
            showToast(
                'Please enter a valid positive deposit amount',
                'warning'
            );
            return;
        }
        try {
            await apiClient.post('/payments/accounts/deposit', {
                userId,
                amount: parseFloat(depositAmount),
            });
            showToast('Account successfully replenished!', 'success');
            setDepositModalOpen(false);
            setDepositAmount('');
            await getBalance(userId);
        } catch (err) {
            showToast(`Error replenishing account: ${err.message}`, 'error');
        }
    };

    const handleCreateOrder = async () => {
        if (
            !newOrder.description ||
            !newOrder.amount ||
            parseFloat(newOrder.amount) <= 0
        ) {
            showToast(
                'Please fill all fields with valid positive values',
                'warning'
            );
            return;
        }
        try {
            await apiClient.post('/orders', {
                userId,
                description: newOrder.description,
                amount: parseFloat(newOrder.amount),
            });
            showToast(
                'Order created successfully! Status will update soon.',
                'info'
            );
            setOrderModalOpen(false);
            setNewOrder({ description: '', amount: '' });
            await getOrders(userId);
        } catch (err) {
            showToast(`Error creating order: ${err.message}`, 'error');
        }
    };

    const handleAmountChange = (e, setter) => {
        const value = e.target.value;
        if (value === '' || parseFloat(value) >= 0) {
            setter(value);
        }
    };

    useEffect(() => {
        let storedUserId = localStorage.getItem('userId');
        if (!storedUserId) {
            storedUserId = crypto.randomUUID();
            localStorage.setItem('userId', storedUserId);
        }
        setUserId(storedUserId);
    }, []);

    useEffect(() => {
        if (userId) {
            const interval = setInterval(() => {
                getBalance(userId);
                getOrders(userId);
            }, 5000);

            getBalance(userId);
            getOrders(userId);

            return () => clearInterval(interval);
        }
    }, [userId, getBalance, getOrders]);

    const getStatusInfo = (status) => {
        return statusConfig[status] || { text: 'Unknown', className: '' };
    };

    return (
        <>
            <style>{`
                @keyframes slideInLeft {
                    from { transform: translateX(-100%); }
                    to { transform: translateX(0); }
                }
                @keyframes slideInRight {
                    from { transform: translateX(100%); }
                    to { transform: translateX(0); }
                }
                @keyframes radialExpand {
                    from { transform: scale(0); opacity: 0; }
                    to { transform: scale(1); opacity: 1; }
                }
                .modal-slide-in-left {
                    animation: slideInLeft 0.3s ease-out;
                }
                .modal-slide-in-right {
                    animation: slideInRight 0.3s ease-out;
                }
                .order-card {
                    animation: radialExpand 0.5s ease-out;
                }
                .hover-scale:hover {
                    transform: scale(1.05);
                    box-shadow: 0 10px 20px rgba(0,0,0,0.3);
                }
            `}</style>
            <div className="min-h-screen bg-gradient-to-br from-indigo-900 via-purple-900 to-blue-900 text-white font-sans">
                <header className="fixed top-0 left-0 right-0 p-4 bg-opacity-50 bg-black z-10">
                    <h1 className="text-3xl font-extrabold text-center tracking-wide">
                        HSE Shop
                    </h1>
                </header>

                <main className="max-w-6xl mx-auto pt-20 pb-8 px-4">
                    <div className="flex justify-center mb-12">
                        <div className="bg-white bg-opacity-10 backdrop-blur-lg rounded-full p-8 shadow-2xl w-72 h-72 flex flex-col items-center justify-center transform hover:scale-110 transition-transform duration-300">
                            <h2 className="text-lg font-bold mb-3">
                                Your Profile
                            </h2>
                            <div className="group relative">
                                <p className="text-xs text-gray-300 mb-2 cursor-pointer">
                                    User ID: {userId?.slice(0, 8)}...
                                </p>
                                <span className="absolute hidden group-hover:block bg-black bg-opacity-80 text-white text-xs rounded p-2 -mt-10 left-1/2 transform -translate-x-1/2 whitespace-nowrap">
                                    {userId}
                                </span>
                            </div>
                            <div className="flex items-center mb-4">
                                <svg
                                    className="w-6 h-6 text-yellow-400 mr-2"
                                    fill="currentColor"
                                    viewBox="0 0 24 24"
                                >
                                    <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2zm0 14h16V8H4v10zm2-8h12v2H6V10zm0 4h8v2H6v-2z" />
                                </svg>
                                <p className="text-2xl font-extrabold">
                                    {loading.balance && balance === 0 ? (
                                        <svg
                                            className="animate-spin h-5 w-5 text-yellow-400"
                                            viewBox="0 0 24 24"
                                        >
                                            <circle
                                                className="opacity-25"
                                                cx="12"
                                                cy="12"
                                                r="10"
                                                stroke="currentColor"
                                                strokeWidth="4"
                                                fill="none"
                                            />
                                            <path
                                                className="opacity-75"
                                                fill="currentColor"
                                                d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"
                                            />
                                        </svg>
                                    ) : (
                                        `${(balance || 0).toFixed(2)} ₽`
                                    )}
                                </p>
                            </div>
                            <button
                                className="bg-yellow-500 text-black py-2 px-4 rounded-full hover:bg-yellow-400 transition hover-scale text-sm"
                                onClick={() => setDepositModalOpen(true)}
                            >
                                <svg
                                    className="w-4 h-4 inline mr-1"
                                    fill="currentColor"
                                    viewBox="0 0 24 24"
                                >
                                    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm-1-13h2v6h-2zm0 8h2v2h-2z" />
                                </svg>
                                Deposit
                            </button>
                        </div>
                    </div>

                    <div className="flex justify-center mb-6">
                        <button
                            className="bg-green-500 text-white py-2 px-4 rounded-full hover:bg-green-400 transition hover-scale text-sm"
                            onClick={() => setOrderModalOpen(true)}
                        >
                            <svg
                                className="w-4 h-4 inline mr-1"
                                fill="currentColor"
                                viewBox="0 0 24 24"
                            >
                                <path d="M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z" />
                            </svg>
                            Create Order
                        </button>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
                        {loading.orders && orders.length === 0 ? (
                            <div className="col-span-full flex justify-center">
                                <svg
                                    className="animate-spin h-8 w-8 text-yellow-400"
                                    viewBox="0 0 24 24"
                                >
                                    <circle
                                        className="opacity-25"
                                        cx="12"
                                        cy="12"
                                        r="10"
                                        stroke="currentColor"
                                        strokeWidth="4"
                                        fill="none"
                                    />
                                    <path
                                        className="opacity-75"
                                        fill="currentColor"
                                        d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"
                                    />
                                </svg>
                            </div>
                        ) : orders.length > 0 ? (
                            orders.map((order, index) => {
                                const statusInfo = getStatusInfo(order.status);
                                return (
                                    <div
                                        key={order.id}
                                        className="bg-white bg-opacity-10 backdrop-blur-lg rounded-xl p-4 order-card hover-scale"
                                        style={{
                                            animationDelay: `${index * 0.1}s`,
                                        }}
                                    >
                                        <div className="flex justify-between items-center mb-2">
                                            <div className="group relative">
                                                <p className="text-gray-300 text-xs break-all cursor-pointer">
                                                    {order.id.slice(0, 8)}...
                                                </p>
                                                <span className="absolute hidden group-hover:block bg-black bg-opacity-80 text-white text-xs rounded p-2 -mt-10 left-1/2 transform -translate-x-1/2 whitespace-nowrap">
                                                    {order.id}
                                                </span>
                                            </div>
                                            <span
                                                className={`px-2 py-1 text-xs font-semibold rounded-full ${statusInfo.className}`}
                                            >
                                                {statusInfo.text}
                                            </span>
                                        </div>
                                        <h3 className="text-base font-medium">
                                            {order.description}
                                        </h3>
                                        <p className="text-yellow-400 text-sm">
                                            {(order.amount || 0).toFixed(2)} ₽
                                        </p>
                                    </div>
                                );
                            })
                        ) : (
                            <p className="col-span-full text-center text-gray-300">
                                No orders yet. Create one to get started!
                            </p>
                        )}
                    </div>
                </main>

                {depositModalOpen && (
                    <div className="fixed inset-0 bg-black bg-opacity-70 flex items-center justify-center z-50">
                        <div className="bg-white bg-opacity-10 backdrop-blur-lg p-6 rounded-xl w-full max-w-sm modal-slide-in-left">
                            <h2 className="text-lg font-semibold mb-4">
                                Deposit Funds
                            </h2>
                            <input
                                type="number"
                                className="w-full p-2 bg-transparent border border-gray-300 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-yellow-500 mb-4"
                                value={depositAmount}
                                onChange={(e) =>
                                    handleAmountChange(e, setDepositAmount)
                                }
                                placeholder="Enter amount"
                                min="0"
                                step="0.01"
                            />
                            <div className="flex justify-end gap-2">
                                <button
                                    className="bg-gray-500 text-white py-1 px-3 rounded-lg hover:bg-gray-400 transition text-sm"
                                    onClick={() => setDepositModalOpen(false)}
                                >
                                    Cancel
                                </button>
                                <button
                                    className="bg-yellow-500 text-black py-1 px-3 rounded-lg hover:bg-yellow-400 transition text-sm"
                                    onClick={handleDeposit}
                                >
                                    Deposit
                                </button>
                            </div>
                        </div>
                    </div>
                )}

                {orderModalOpen && (
                    <div className="fixed inset-0 bg-black bg-opacity-70 flex items-center justify-center z-50">
                        <div className="bg-white bg-opacity-10 backdrop-blur-lg p-6 rounded-xl w-full max-w-sm modal-slide-in-right">
                            <h2 className="text-lg font-semibold mb-4">
                                New Order
                            </h2>
                            <input
                                className="w-full p-2 bg-transparent border border-gray-300 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 mb-4"
                                value={newOrder.description}
                                onChange={(e) =>
                                    setNewOrder({
                                        ...newOrder,
                                        description: e.target.value,
                                    })
                                }
                                placeholder="Description"
                            />
                            <input
                                type="number"
                                className="w-full p-2 bg-transparent border border-gray-300 rounded-lg text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 mb-4"
                                value={newOrder.amount}
                                onChange={(e) =>
                                    handleAmountChange(e, (value) =>
                                        setNewOrder({
                                            ...newOrder,
                                            amount: value,
                                        })
                                    )
                                }
                                placeholder="Amount"
                                min="0"
                                step="0.01"
                            />
                            <div className="flex justify-end gap-2">
                                <button
                                    className="bg-gray-500 text-white py-1 px-3 rounded-lg hover:bg-gray-400 transition text-sm"
                                    onClick={() => setOrderModalOpen(false)}
                                >
                                    Cancel
                                </button>
                                <button
                                    className="bg-green-500 text-white py-1 px-3 rounded-lg hover:bg-green-400 transition text-sm"
                                    onClick={handleCreateOrder}
                                >
                                    Create
                                </button>
                            </div>
                        </div>
                    </div>
                )}

                <ToastContainer />
            </div>
        </>
    );
}

export default App;
